using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 從 GlobalObjectId 連結定位物件並匯出。
    ///
    /// 存在的理由：專案裡「指某個 scene 物件」的通用交換格式是 BugReportUtility 產的
    /// markdown 連結（`[名稱](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-…)`），
    /// 人可以貼給 Unity 直接跳過去，但那個 id 本身不含節點路徑 —— 拿到連結的一方沒有
    /// 任何辦法反推出 `up scene ls --node` 要填什麼。這裡把「連結 → 物件 → 文字」補上，
    /// 所以貼一條連結就等於指定了一個節點。
    ///
    /// Unity 原生的 <see cref="GlobalObjectId.GlobalObjectIdentifierToObjectSlow"/> 只認
    /// **已載入 scene** 裡的物件 —— Prefab Stage 裡的不算、沒開著的 prefab 更不算。
    /// 但 prefab 連結的 targetObjectId 就是 imported asset 裡的 local fileID，
    /// 所以這裡對 .prefab 自己走 <see cref="ResolveInPrefab"/>：先掃開著的 stage
    /// （比對 GetGlobalObjectIdSlow，精確含 nested），再掃 AssetDatabase 載進來的
    /// prefab asset（比對 TryGetGUIDAndLocalFileIdentifier）。**不需要開 Prefab Stage**，
    /// 貼一條連結一次就拿到內容。scene 物件仍受 Unity 限制：解不開時把 guid 翻成
    /// scene 路徑告訴呼叫端要先開哪個 scene。
    /// </summary>
    public static class EditGid
    {
        // 從任意貼上的文字裡撈出 id：markdown 連結、裸 URL、只有 id 本身都吃
        private static readonly Regex GidRe = new(
            @"GlobalObjectId_V1-(\d+)-([0-9a-fA-F]{32})-(\d+)-(\d+)", RegexOptions.Compiled);

        /// <param name="token">含 GlobalObjectId 的任意文字（markdown 連結 / URL / 裸 id）</param>
        /// <param name="subPath">從命中的物件再往下鑽的相對路徑；留空 = 就從它自己開始</param>
        /// <param name="depth">往下幾層；-1 = 交給 charBudget 決定</param>
        /// <param name="fullExpand">不摺疊已知子樹、不排除視覺 component</param>
        /// <param name="charBudget">輸出上限；超標就自動加深摺疊。0 = 不限</param>
        /// <param name="includeFsm">附上 FSM markdown 段</param>
        /// <param name="openScene">物件所在 scene 沒開著時，允許幫忙開（會換掉當前 scene）</param>
        /// <param name="select">同時在 Unity 裡選中並 ping 它</param>
        /// <param name="fsmOnly">只輸出 FSM，不重複 hierarchy</param>
        /// <param name="structureOnly">只輸出 hierarchy 結構，不輸出 component 欄位或 FSM</param>
        public static string Peek(
            string token, string subPath = null, int depth = -1, bool fullExpand = true,
            int charBudget = PrefabTextReader.DefaultCharBudget, bool includeFsm = false,
            bool openScene = false, bool select = false,
            bool fsmOnly = false, bool structureOnly = false)
        {
            if (fsmOnly && structureOnly)
                return PrefabTextReader.HardCap(
                    "# fsmOnly 與 structureOnly 不能同時開啟\n", charBudget);

            var match = GidRe.Match(token ?? "");
            if (!match.Success)
                return PrefabTextReader.HardCap(
                    "# 沒有 GlobalObjectId：貼上的內容裡找不到 GlobalObjectId_V1-… 片段\n" +
                    "# 期望像這樣：[名稱](http://localhost:8888/webhook?globalId=" +
                    "GlobalObjectId_V1-2-<32位guid>-<objectId>-<prefabId>)", charBudget);

            var gidStr = match.Value;
            if (!GlobalObjectId.TryParse(gidStr, out var gid))
                return PrefabTextReader.HardCap(
                    $"# GlobalObjectId 格式對但 Unity 解析失敗：{gidStr}", charBudget);

            var assetPath = AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);
            var obj = Resolve(gid, assetPath, openScene, out var note);
            if (obj == null)
                return PrefabTextReader.HardCap(
                    Unresolved(gid, gidStr, assetPath, note), charBudget);

            if (select)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            var go = obj as GameObject ?? (obj as Component)?.gameObject;
            if (go == null)
                return PrefabTextReader.HardCap(AssetSummary(obj, gidStr), charBudget);

            var header = new StringBuilder();
            header.AppendLine($"# gid: {gidStr}");
            header.AppendLine($"# owner: {Owner(go, assetPath)}");
            header.AppendLine($"# node: {HierarchyPath(go.transform)}");
            if (obj is Component comp)
                header.AppendLine($"# 連結指的是 component: {comp.GetType().Name}");
            header.Append(NextCommand(go, assetPath, exported: true));

            var root = go.transform;
            if (!string.IsNullOrEmpty(subPath))
            {
                var found = EditResolve.TryNode(root, subPath);
                if (found == null)
                    return PrefabTextReader.HardCap(
                        header + $"# 找不到子路徑 {subPath}；" +
                        EditResolve.DescribeChildren(root, subPath), charBudget);
                root = found;
                header.AppendLine($"# subtree: {subPath}");
            }

            return PrefabTextReader.ExportResolvedNode(
                root.gameObject, depth, fullExpand, charBudget, header,
                includeFsm, fsmOnly, structureOnly);
        }

        /// <summary>只回「這條連結指到誰」，不匯出內容 —— 想接著用 up scene ls / refs 時夠用。</summary>
        public static string Locate(string token, bool openScene = false, bool select = false)
        {
            var match = GidRe.Match(token ?? "");
            if (!match.Success) return "# 沒有 GlobalObjectId";
            var gidStr = match.Value;
            if (!GlobalObjectId.TryParse(gidStr, out var gid))
                return $"# 解析失敗：{gidStr}";

            var assetPath = AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);
            var obj = Resolve(gid, assetPath, openScene, out var note);
            if (obj == null) return Unresolved(gid, gidStr, assetPath, note);

            if (select)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            var go = obj as GameObject ?? (obj as Component)?.gameObject;
            if (go == null) return AssetSummary(obj, gidStr);

            var comps = string.Join(" ", go.GetComponents<Component>()
                .Where(c => c != null).Select(c => c.GetType().Name));
            return $"# owner: {Owner(go, assetPath)}\n" +
                   $"{HierarchyPath(go.transform)}\n" +
                   $"  <{comps}>\n" +
                   $"  (+{Descendants(go.transform)} nodes){(go.activeSelf ? "" : "  ~inactive")}\n" +
                   NextCommand(go, assetPath);
        }

        /// <summary>
        /// 連結 → Object 的唯一入口。順序：Unity 原生（已載入 scene）→ prefab 自己掃
        /// → scene 才考慮開檔。prefab 一律不開 stage：imported asset 就足夠匹配。
        /// </summary>
        private static Object Resolve(
            GlobalObjectId gid, string assetPath, bool openScene, out string note)
        {
            note = null;
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (obj != null) return obj;
            if (string.IsNullOrEmpty(assetPath))
            {
                note = "guid 對不到任何資產（不在這個 repo？已刪除？）";
                return null;
            }

            if (assetPath.EndsWith(".prefab"))
            {
                // --open 對 prefab 的意義是「順便把 stage 打開」，不是解析的前提
                if (openScene) TryOpenPrefabStage(assetPath, true, out note);
                return ResolveInPrefab(gid, assetPath, out note);
            }

            if (TryOpenOwnerScene(assetPath, openScene, out note))
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            return obj;
        }

        /// <summary>
        /// prefab 裡的物件不靠 Unity 原生解析。兩條路：
        /// (1) 該 prefab 的 Stage 正開著 → 對 stage 裡每個 GameObject / Component 算
        ///     GetGlobalObjectIdSlow 比對，和 BugReportUtility 產連結時算的是同一個函式，
        ///     所以連 nested prefab instance 內的物件（targetPrefabId != 0）都精確命中。
        /// (2) 沒開 → 載 imported asset，比對 TryGetGUIDAndLocalFileIdentifier 的 local id。
        ///     原生物件 local id == targetObjectId；nested instance 內的物件則要
        ///     instance handle 的 id == targetPrefabId 且 source 端物件 id == targetObjectId。
        /// 回的是 asset 物件（不在任何 scene 裡），PrefabTextReader 匯出時和
        /// `up prefab read` 走的是同一顆，輸出格式一致。
        /// </summary>
        private static Object ResolveInPrefab(GlobalObjectId gid, string assetPath, out string note)
        {
            note = null;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == assetPath && stage.prefabContentsRoot != null)
            {
                foreach (var o in AllObjects(stage.prefabContentsRoot))
                    if (SameGid(GlobalObjectId.GetGlobalObjectIdSlow(o), gid)) return o;
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                note = $"載不進 {assetPath}（不是 prefab？）";
                return null;
            }

            var wantObj = unchecked((long)gid.targetObjectId);
            var wantPrefab = unchecked((long)gid.targetPrefabId);
            foreach (var o in AllObjects(asset))
            {
                if (wantPrefab == 0)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out _, out long localId)
                        && localId == wantObj) return o;
                    continue;
                }

                var handle = PrefabUtility.GetPrefabInstanceHandle(o);
                if (handle == null) continue;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(handle, out _, out long hid)
                    || hid != wantPrefab) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(o);
                if (src != null
                    && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(src, out _, out long sid)
                    && sid == wantObj) return o;
            }

            note = $"{assetPath} 裡沒有 fileID={wantObj}" +
                   (wantPrefab != 0 ? $"（instance {wantPrefab}）" : "") +
                   " 的物件 —— 已被刪除、搬到別的 prefab，或連結是舊的";
            return null;
        }

        private static bool SameGid(GlobalObjectId a, GlobalObjectId b) =>
            a.identifierType == b.identifierType && a.assetGUID == b.assetGUID
            && a.targetObjectId == b.targetObjectId && a.targetPrefabId == b.targetPrefabId;

        /// <summary>子樹裡所有 GameObject 與 Component（含 inactive）；missing script 跳過。</summary>
        private static System.Collections.Generic.IEnumerable<Object> AllObjects(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                yield return t.gameObject;
                foreach (var c in t.GetComponents<Component>())
                    if (c != null) yield return c;
            }
        }

        /// <summary>
        /// 物件在哪個容器裡。三種都會遇到，而且要分得出來，因為後續指令不一樣：
        /// scene 物件接 `up scene ls`，prefab 裡的接 `up prefab read`。
        /// 注意 **Prefab Stage 開著時 go.scene.path 會是那個 .prefab 的路徑**（隔離場景），
        /// 照字面印成「scene X.prefab」會讓人以為場上有這個物件。
        /// </summary>
        private static string Owner(GameObject go, string assetPath)
        {
            var path = go.scene.IsValid() ? go.scene.path : null;
            if (!string.IsNullOrEmpty(path))
                return path.EndsWith(".prefab")
                    ? $"prefab stage {path}（正開著編輯，不是場上的物件）"
                    : "scene " + path;
            if (go.scene.IsValid()) return "scene " + go.scene.name + "（未存檔）";
            return "prefab " + (string.IsNullOrEmpty(assetPath) ? "(unknown)" : assetPath);
        }

        /// <summary>從 scene root 起算的完整路徑，可以直接餵給 up scene ls --node。</summary>
        private static string HierarchyPath(Transform t) => HierarchyPath(t, false);

        /// <param name="skipRoot">切掉最上層那一段 —— prefab 側的 `--node` 是不含 root 的相對路徑</param>
        private static string HierarchyPath(Transform t, bool skipRoot)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var cur = t; cur != null; cur = cur.parent) parts.Add(cur.name);
            parts.Reverse();
            if (skipRoot && parts.Count > 0) parts.RemoveAt(0);
            return string.Join("/", parts);
        }

        /// <summary>
        /// 「拿到這條連結之後該下哪一條指令」——連結本身只是個 id，
        /// 呼叫端真正要的是能接著跑的東西，而 scene 與 prefab 兩側的 `--node` 語意不同
        /// （scene 含 root object 名、prefab 不含 root），少印這一行就等於要對方自己猜一次。
        /// 路徑用單引號包，直接複製到 shell 就能跑。
        /// </summary>
        /// <param name="exported">Peek 已把欄位印在下方 —— 這時「接著用」不能再指向 read
        /// （agent 實測會以為 read 才是拿欄位的正解、再多打一次），改成列「下鑽」與
        /// 「單顆 component 完整值」兩條真正的後續路。</param>
        private static string NextCommand(GameObject go, string assetPath, bool exported = false)
        {
            var scenePath = go.scene.IsValid() ? go.scene.path : null;
            var isPrefab = !go.scene.IsValid()
                           || (!string.IsNullOrEmpty(scenePath) && scenePath.EndsWith(".prefab"));
            if (isPrefab)
            {
                var owner = !string.IsNullOrEmpty(scenePath) && scenePath.EndsWith(".prefab")
                    ? scenePath
                    : assetPath;
                if (string.IsNullOrEmpty(owner)) return "";
                var rel = HierarchyPath(go.transform, true);
                var nodeArg = string.IsNullOrEmpty(rel) ? "" : $" --node '{rel}'";
                if (exported)
                    return $"# 欄位已在下方（葉節點含預設值）。下鑽子節點：up prefab read '{owner}' --node '{rel}/<子節點>'；" +
                           $"單顆 component 完整值：up prefab peek '{owner}'{nodeArg} --comp <型別> --deep\n";
                return $"# 接著用：up prefab read '{owner}'{nodeArg}\n";
            }

            var scenePathArg = HierarchyPath(go.transform);
            if (exported)
                return $"# 欄位已在下方。下鑽：up scene ls --node '{scenePathArg}/<子節點>'；" +
                       $"runtime 值：up peek '{scenePathArg}' <型別>\n";
            return $"# 接著用：up scene ls --node '{scenePathArg}'\n";
        }

        private static int Descendants(Transform t)
        {
            var n = 0;
            foreach (Transform c in t) n += 1 + Descendants(c);
            return n;
        }

        /// <summary>
        /// 解不開時盡量把「為什麼」與「下一步」講完 —— identifierType 決定了物件在哪種容器裡，
        /// 而 scene 物件的 assetGUID 就是那個 scene，所以答案通常是「先開那個 scene」。
        /// </summary>
        private static string Unresolved(
            GlobalObjectId gid, string gidStr, string assetPath, string note)
        {
            var kind = gid.identifierType switch
            {
                0 => "null（連結指向的物件當時就是空的）",
                1 => "imported asset（prefab / asset 裡的物件）",
                2 => "scene object",
                3 => "source asset",
                _ => "unknown"
            };

            var sb = new StringBuilder();
            sb.AppendLine($"# 解不開這個 GlobalObjectId：{gidStr}");
            sb.AppendLine($"# identifierType={gid.identifierType} → {kind}");
            sb.AppendLine($"# 來源資產：{(string.IsNullOrEmpty(assetPath) ? "guid 對不到任何資產（不在這個 repo？已刪除？）" : assetPath)}");
            if (!string.IsNullOrEmpty(note)) sb.AppendLine("# " + note);
            return sb.ToString();
        }

        /// <summary>
        /// 只在 --open 時被呼叫：解析本身不需要 stage（見 ResolveInPrefab），這裡純粹是
        /// 使用者想順便在 Editor 裡打開那個 prefab。dirty 的 stage 不換，理由同 scene。
        /// </summary>
        private static bool TryOpenPrefabStage(string assetPath, bool allowOpen, out string note)
        {
            note = null;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == assetPath) return true;
            if (!allowOpen) return false;

            if (stage != null && stage.scene.isDirty)
            {
                note = $"目前開著的 prefab 編輯模式有未存檔的改動（{stage.assetPath}），" +
                       "不自動換；先存檔或自己開";
                return false;
            }

            var opened = PrefabStageUtility.OpenPrefab(assetPath);
            if (opened == null)
            {
                note = $"開不起來 {assetPath}（檔案不存在或不是 prefab？）";
                return false;
            }

            note = $"已開啟 prefab 編輯模式 {assetPath}";
            return true;
        }

        private static string AssetSummary(Object obj, string gidStr)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            return $"# gid: {gidStr}\n" +
                   $"# 這條連結指的不是 GameObject，是 {obj.GetType().Name}：{obj.name}\n" +
                   (string.IsNullOrEmpty(path)
                       ? "# （不是資產）\n"
                       : $"{path}\n# 內容用 up asset fields \"{path}\" 看\n");
        }

        /// <summary>
        /// 物件只有它所在的容器開著才解得開。要不要幫忙開是呼叫端的決定 ——
        /// 換 scene / 換 prefab stage 會丟掉未存檔的編輯，所以 dirty 的時候一律拒絕，
        /// 不猜使用者想不想留。
        /// </summary>
        private static bool TryOpenOwnerScene(string assetPath, bool allowOpen, out string note)
        {
            note = null;
            if (string.IsNullOrEmpty(assetPath)) return false;
            // .prefab 在 Resolve 就被 ResolveInPrefab 接走了，不會走到這；留著防呆
            if (assetPath.EndsWith(".prefab"))
                return TryOpenPrefabStage(assetPath, allowOpen, out note);
            if (!assetPath.EndsWith(".unity"))
            {
                note = "來源既不是 scene 也不是 prefab；物件可能已從那份資產裡刪掉了";
                return false;
            }

            if (SceneManager.GetSceneByPath(assetPath).isLoaded)
            {
                note = "這個 scene 已經開著，但物件找不到 —— 可能已被刪除或搬走";
                return false;
            }

            if (!allowOpen)
            {
                note = $"物件所在的 scene 沒開著。先 up scene open \"{assetPath}\"，或這次加 --open";
                return false;
            }

            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    note = $"有未存檔的 scene（{SceneManager.GetSceneAt(i).name}），不自動換 scene；" +
                           "先 up scene save 或自己開";
                    return false;
                }

            EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
            note = $"已開啟 {assetPath}";
            return true;
        }
    }
}
