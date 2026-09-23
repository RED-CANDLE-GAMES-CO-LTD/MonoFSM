using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// Asset 層級的引用反查（`up asset-refs`）與「為什麼這顆 asset 會進 build」（`up why-in-build`）。
    ///
    /// 為什麼另外開一支而不是塞進 EditRefs：EditRefs 是「同一顆 prefab / scene 內」節點對節點的引用，
    /// 這裡是跨 asset 的依賴邊（AssetDatabase.GetDependencies），範圍是整個 Assets/ + Packages/。
    /// 兩者只有「最後一跳是哪個 Component.propertyPath」這段邏輯相近，但 EditRefs 比的是 scene 物件，
    /// 這裡比的是 asset（含 sub-asset，例如 fbx 裡的 Mesh），所以各自實作。
    ///
    /// 陷阱：GetDependencies 回的是 Editor 端依賴，editor-only 欄位也算（例如 TMP Static 字型的 source font）
    /// —— 結果是「可能進 build」的上界，不是 BuildReport 的實際內容。
    /// </summary>
    public static class AssetDeps
    {
        private const int MaxHitsPerReferrer = 8;

        /// <summary>引用一定是 0 的檔案類型 —— 全庫掃描時跳過，省掉大部分 GetDependencies 呼叫。</summary>
        private static readonly HashSet<string> LeafExt = new()
        {
            ".cs", ".dll", ".asmdef", ".asmref", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr",
            ".tif", ".tiff", ".bmp", ".gif", ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".otf", ".ttf",
            ".txt", ".json", ".md", ".bytes", ".xml", ".csv", ".hlsl", ".cginc", ".so", ".a", ".bundle",
            ".jar", ".aar", ".h", ".m", ".mm", ".cpp", ".c", ".pdf", ".html", ".js", ".uss", ".yaml",
        };

        /// <summary>陣列元素是這些型別就不往下走（不可能含 object reference，TerrainData 之類會爆量）。</summary>
        private static readonly HashSet<string> PrimitiveElem = new()
        {
            "int", "float", "double", "bool", "char", "UInt8", "SInt8", "UInt16", "SInt16", "UInt32",
            "SInt32", "UInt64", "SInt64", "unsigned int", "Vector2f", "Vector3f", "Vector4f", "Quaternionf",
            "ColorRGBA", "Matrix4x4f", "Rectf", "AABB", "string",
        };

        // ───────────────────────── asset-refs ─────────────────────────

        /// <param name="token">asset 路徑或 32 hex guid（Python 端已從 webhook 連結抽出 guid）</param>
        /// <param name="limit">referrer 只列前幾個</param>
        /// <param name="all">true = 全部列出</param>
        public static string AssetRefs(string token, int limit = 20, bool all = false)
        {
            var target = ResolvePath(token, out var err);
            if (target == null) return err;

            var referrers = new List<string>();
            foreach (var p in AssetDatabase.GetAllAssetPaths())
            {
                if (p == target || !IsScannable(p)) continue;
                if (System.Array.IndexOf(AssetDatabase.GetDependencies(p, false), target) >= 0)
                    referrers.Add(p);
            }
            referrers.Sort(System.StringComparer.Ordinal);

            var sb = new StringBuilder();
            sb.AppendLine($"# {target} 被 {referrers.Count} 個 asset 直接引用");
            if (referrers.Count == 0)
            {
                sb.AppendLine("（沒有 asset 直接引用它；要看是不是 build root 用 `up why-in-build`）");
                return sb.ToString();
            }

            var ids = TargetIds(target);
            var shown = all ? referrers.Count : Mathf.Min(limit, referrers.Count);
            for (var i = 0; i < shown; i++)
            {
                sb.AppendLine(referrers[i]);
                AppendFieldHits(sb, referrers[i], ids, "    ");
            }
            if (shown < referrers.Count)
                sb.AppendLine($"# 還有 {referrers.Count - shown} 個，用 --all 看全部");
            return sb.ToString();
        }

        // ───────────────────────── why-in-build ─────────────────────────

        public static string WhyInBuild(string token, int limit = 10, bool all = false)
        {
            var target = ResolvePath(token, out var err);
            if (target == null) return err;

            var sb = new StringBuilder();
            var rootKind = new Dictionary<string, string>();
            var rootSummary = CollectRoots(rootKind);

            var parent = new Dictionary<string, string>();
            var deps = new Dictionary<string, string[]>();
            var q = new Queue<string>();
            foreach (var kv in rootKind)
            {
                parent[kv.Key] = null;
                q.Enqueue(kv.Key);
            }
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                var d = AssetDatabase.GetDependencies(cur, false);
                deps[cur] = d;
                foreach (var x in d)
                {
                    if (x == cur || parent.ContainsKey(x) || IsCode(x)) continue;
                    parent[x] = cur;
                    q.Enqueue(x);
                }
            }

            sb.AppendLine($"# target: {target}");
            sb.AppendLine($"# roots: {rootSummary}；展開到 {parent.Count} 個 asset");
            if (!parent.ContainsKey(target))
            {
                sb.AppendLine("不在 build 裡（沒有任何 build root 經依賴鏈走到它）");
                return sb.ToString();
            }
            if (parent[target] == null)
            {
                sb.AppendLine($"它本身就是 build root：[{rootKind[target]}]");
                return sb.ToString();
            }

            var chain = new List<string>();
            for (var c = target; c != null; c = parent[c]) chain.Add(c);
            chain.Reverse();
            sb.AppendLine("最短鏈：");
            sb.AppendLine($"  [{rootKind[chain[0]]}] {chain[0]}");
            for (var i = 1; i < chain.Count; i++) sb.AppendLine($"  -> {chain[i]}");

            var last = chain[chain.Count - 2];
            sb.AppendLine($"最後一跳（{last} 裡指到它的欄位）：");
            AppendFieldHits(sb, last, TargetIds(target), "    ");

            // 要斷乾淨得把所有直接 referrer 都斷掉，所以把 build 內其他直接 referrer 也列出來
            var others = new List<string>();
            foreach (var kv in deps)
                if (kv.Key != last && kv.Key != target && System.Array.IndexOf(kv.Value, target) >= 0)
                    others.Add(kv.Key);
            others.Sort(System.StringComparer.Ordinal);
            if (others.Count == 0)
            {
                sb.AppendLine("build 內沒有其他直接 referrer（斷掉上面這一跳就出 build）");
            }
            else
            {
                sb.AppendLine($"build 內另外還有 {others.Count} 個直接 referrer（要全部斷掉才會出 build）：");
                var shown = all ? others.Count : Mathf.Min(limit, others.Count);
                for (var i = 0; i < shown; i++) sb.AppendLine($"  {others[i]}");
                if (shown < others.Count)
                    sb.AppendLine($"# 還有 {others.Count - shown} 個，用 --all 看全部（欄位細節用 `up asset-refs`）");
            }
            sb.AppendLine("# 註：依賴來自 AssetDatabase.GetDependencies，editor-only 欄位也算，是上界不是 BuildReport");
            return sb.ToString();
        }

        /// <summary>收集 build root，回一行摘要。Active Build Profile 有 override scene list 就用它。</summary>
        private static string CollectRoots(Dictionary<string, string> kind)
        {
            int nScene = 0, nRes = 0, nPre = 0, nAddr = 0;
            var sceneSrc = "EditorBuildSettings";
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            var prof = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
            if (prof != null && prof.overrideGlobalScenes)
            {
                scenes = prof.scenes;
                sceneSrc = "BuildProfile " + AssetDatabase.GetAssetPath(prof);
            }
            foreach (var s in scenes)
                if (s.enabled && Add(kind, s.path, "Scene")) nScene++;

            foreach (var p in AssetDatabase.GetAllAssetPaths())
                if (IsResourcesAsset(p) && Add(kind, p, "Resources")) nRes++;

            foreach (var o in PlayerSettings.GetPreloadedAssets())
                if (o != null && Add(kind, AssetDatabase.GetAssetPath(o), "Preloaded")) nPre++;

            nAddr = CollectAddressables(kind);
            return $"{nScene} scene（{sceneSrc}）+ {nRes} Resources + {nPre} Preloaded"
                   + (nAddr < 0 ? "（沒裝 Addressables）" : $" + {nAddr} Addressables");
        }

        /// <summary>用反射讀 Addressables，免得 MonoFSM 的 asmdef 硬依賴它。回 -1 = 沒裝。</summary>
        private static int CollectAddressables(Dictionary<string, string> kind)
        {
            var t = System.Type.GetType(
                "UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            if (t == null) return -1;
            var settings = t.GetProperty("Settings")?.GetValue(null);
            if (settings == null) return 0;
            var n = 0;
            if (settings.GetType().GetProperty("groups")?.GetValue(settings) is not IEnumerable groups) return 0;
            foreach (var g in groups)
            {
                if (g == null) continue;
                var gName = g.GetType().GetProperty("Name")?.GetValue(g) as string;
                if (g.GetType().GetProperty("entries")?.GetValue(g) is not IEnumerable entries) continue;
                foreach (var e in entries)
                {
                    var path = e?.GetType().GetProperty("AssetPath")?.GetValue(e) as string;
                    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                        continue; // Built In Data 那種虛擬 entry
                    var label = $"Addressables:{gName}";
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        foreach (var guid in AssetDatabase.FindAssets("", new[] { path }))
                        {
                            var sub = AssetDatabase.GUIDToAssetPath(guid);
                            if (!AssetDatabase.IsValidFolder(sub) && Add(kind, sub, label)) n++;
                        }
                    }
                    else if (Add(kind, path, label)) n++;
                }
            }
            return n;
        }

        private static bool Add(Dictionary<string, string> kind, string path, string label)
        {
            if (string.IsNullOrEmpty(path) || kind.ContainsKey(path)) return false;
            kind[path] = label;
            return true;
        }

        private static bool IsResourcesAsset(string p)
        {
            if (!p.Contains("/Resources/") || p.Contains("/Editor/") || IsCode(p)) return false;
            return !AssetDatabase.IsValidFolder(p);
        }

        // ───────────────────────── 最後一跳：哪個欄位 ─────────────────────────

        /// <summary>把 referrer 裡「指到 target（含 sub-asset）」的欄位一行一個寫出來。</summary>
        private static void AppendFieldHits(StringBuilder sb, string referrer, HashSet<int> ids, string indent)
        {
            var hits = new List<string>();
            var note = "";
            if (referrer.EndsWith(".unity")) note = SceneHits(referrer, ids, hits);
            else if (referrer.EndsWith(".prefab")) PrefabHits(referrer, ids, hits);
            else AssetHits(referrer, ids, hits);

            if (!string.IsNullOrEmpty(note)) sb.AppendLine(indent + note);
            if (hits.Count == 0)
            {
                if (string.IsNullOrEmpty(note))
                    sb.AppendLine(indent + "（找不到指過去的欄位：可能在 scene 層級設定 / importer / 巢狀 prefab 內部）");
                return;
            }
            var shown = Mathf.Min(MaxHitsPerReferrer, hits.Count);
            for (var i = 0; i < shown; i++) sb.AppendLine(indent + hits[i]);
            if (shown < hits.Count) sb.AppendLine($"{indent}…還有 {hits.Count - shown} 處");
        }

        private static void PrefabHits(string path, HashSet<int> ids, List<string> hits)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) return;
            foreach (var c in root.GetComponentsInChildren<Component>(true))
                if (c != null) ScanComponent(c, RelPath(root.transform, c.transform), ids, hits);
        }

        /// <summary>scene 沒開時：非 Play Mode 暫時 additive 開起來查完關掉；Play Mode 開不了就回說明。</summary>
        private static string SceneHits(string path, HashSet<int> ids, List<string> hits)
        {
            var scene = SceneManager.GetSceneByPath(path);
            var opened = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return "（scene 沒開、Play Mode 中不能暫開，看不到欄位；停掉 Play 或先開這個 scene 再跑）";
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                opened = true;
            }
            try
            {
                foreach (var go in scene.GetRootGameObjects())
                foreach (var c in go.GetComponentsInChildren<Component>(true))
                    if (c != null) ScanComponent(c, FullPath(c.transform), ids, hits);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            return opened ? "（scene 原本沒開，暫時 additive 開啟查完已關閉）" : "";
        }

        private static void AssetHits(string path, HashSet<int> ids, List<string> hits)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o == null || o is GameObject || o is Component) continue;
                ScanObject(o, $"{o.name} ({o.GetType().Name})", ids, hits);
            }
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null) ScanObject(importer, $"importer ({importer.GetType().Name})", ids, hits);
        }

        private static void ScanComponent(Component c, string nodePath, HashSet<int> ids, List<string> hits) =>
            ScanObject(c, $"{nodePath} [{c.GetType().Name}", ids, hits, true);

        private static void ScanObject(Object o, string owner, HashSet<int> ids, List<string> hits,
            bool bracket = false)
        {
            var so = new SerializedObject(o);
            var it = so.GetIterator();
            var enter = true;
            while (it.Next(enter))
            {
                enter = ShouldEnter(it);
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (!ids.Contains(it.objectReferenceInstanceIDValue)) continue;
                var ov = it.prefabOverride ? " (prefab override)" : "";
                var refName = it.objectReferenceValue != null ? it.objectReferenceValue.name : "?";
                hits.Add(bracket
                    ? $"{owner}.{it.propertyPath}] -> {refName}{ov}"
                    : $"{owner}.{it.propertyPath} -> {refName}{ov}");
            }
        }

        private static bool ShouldEnter(SerializedProperty p)
        {
            if (p.propertyType != SerializedPropertyType.Generic) return false;
            if (!p.isArray) return true;
            return p.arraySize > 0 && !PrimitiveElem.Contains(p.arrayElementType);
        }

        // ───────────────────────── 共用 ─────────────────────────

        private static string ResolvePath(string token, out string error)
        {
            error = null;
            token = (token ?? "").Trim();
            string path = token;
            if (token.Length == 32 && IsHex(token))
            {
                path = AssetDatabase.GUIDToAssetPath(token);
                if (string.IsNullOrEmpty(path))
                {
                    error = $"# 找不到 guid {token}";
                    return null;
                }
            }
            var g = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(g))
            {
                error = $"# 找不到 asset: {path}（先用 `up guid <關鍵字>` 找正確路徑）";
                return null;
            }
            // 使用者在 Finder / git 刪掉檔案後、Unity refresh 之前，AssetDatabase 還查得到 guid，
            // 但引用它的欄位已經變 missing —— 不擋的話會回「不在 build 裡」，看起來像是查錯（2026-09-23 踩過）
            if (path.StartsWith("Assets/") && !System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
            {
                error = $"# 磁碟上已經沒有 {path}（被刪了，AssetDatabase 還沒 refresh），不用再查";
                return null;
            }
            // 經 guid 繞一圈換成 AssetDatabase 自己的寫法：CLI 傳進來的中文路徑 Unicode 正規化（NFC/NFD）
            // 可能跟 GetDependencies 回的不同，AssetPathToGUID 容忍、字串比對不容忍（實測 July_lake 被判成不在 build）
            return AssetDatabase.GUIDToAssetPath(g);
        }

        private static bool IsHex(string s)
        {
            foreach (var ch in s)
                if (!(ch >= '0' && ch <= '9' || ch >= 'a' && ch <= 'f' || ch >= 'A' && ch <= 'F'))
                    return false;
            return true;
        }

        /// <summary>target 本身 + 所有 sub-asset（fbx 的 Mesh / Material 等）的 instanceID。</summary>
        private static HashSet<int> TargetIds(string target)
        {
            var ids = new HashSet<int>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(target))
                if (o != null) ids.Add(o.GetInstanceID());
            var main = AssetDatabase.LoadMainAssetAtPath(target);
            if (main != null) ids.Add(main.GetInstanceID());
            return ids;
        }

        private static bool IsScannable(string p)
        {
            if (!p.StartsWith("Assets/") && !p.StartsWith("Packages/")) return false;
            var ext = System.IO.Path.GetExtension(p).ToLowerInvariant();
            if (ext.Length == 0 || LeafExt.Contains(ext)) return false; // 沒副檔名多半是資料夾
            return true;
        }

        private static bool IsCode(string p) => p.EndsWith(".cs") || p.EndsWith(".dll");

        private static string RelPath(Transform root, Transform t)
        {
            if (t == root) return "(root)";
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        private static string FullPath(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
