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
    /// GlobalObjectId 只在**物件所在的 scene 開著**時才解得開（Unity 的限制，不是這裡的）。
    /// 解不開時不是回一句失敗，而是把 guid 翻成 scene 路徑告訴呼叫端要先開哪個 scene。
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
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);

            if (obj == null)
            {
                if (TryOpenOwnerScene(assetPath, openScene, out var note))
                    obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj == null)
                    return PrefabTextReader.HardCap(
                        Unresolved(gid, gidStr, assetPath, note), charBudget);
            }

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
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (obj == null)
            {
                if (TryOpenOwnerScene(assetPath, openScene, out var note))
                    obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj == null) return Unresolved(gid, gidStr, assetPath, note);
            }

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
                   $"  (+{Descendants(go.transform)} nodes){(go.activeSelf ? "" : "  ~inactive")}";
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
        private static string HierarchyPath(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var cur = t; cur != null; cur = cur.parent) parts.Add(cur.name);
            parts.Reverse();
            return string.Join("/", parts);
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
        /// scene 物件只有那個 scene 開著才解得開。要不要幫忙開是呼叫端的決定 ——
        /// 換 scene 會丟掉未存檔的編輯，所以 dirty 的時候一律拒絕，不猜使用者想不想留。
        /// </summary>
        private static bool TryOpenOwnerScene(string assetPath, bool allowOpen, out string note)
        {
            note = null;
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (!assetPath.EndsWith(".unity"))
            {
                note = "來源不是 scene；物件可能已從那份資產裡刪掉了";
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
