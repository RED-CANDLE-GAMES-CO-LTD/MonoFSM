using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoFSM.Core.PrefabCache;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    // 把掛了 PrefabTextCacheMarker 的 prefab 匯出成文字 cache，落在 repo 的
    // CacheRoot 底下（鏡射 asset path）。刻意不寫時間戳 —— 存檔頻繁，
    // 內容沒變就不該動檔案。
    //
    // 輸出目錄與「哪些 component 算純視覺」都是專案決定的，由專案端注入
    // （見 CacheRoot / VisualComponents）。
    public static class PrefabTextCacheWriter
    {
        // 相對 repo root 的輸出目錄。專案端可在 [InitializeOnLoadMethod] 改掉。
        public static string CacheRoot = "Tools/prefab-cache";

        // 純視覺 component：對「讀邏輯」沒有貢獻，卻佔掉 PPlayer.md 三成以上。
        // _excludeComponents 走 IsAssignableFrom，所以填 base type 就涵蓋全部子類
        // （Renderer 一項 = Mesh / Skinned / ParticleSystem / Line / Trail Renderer）。
        // 專案特有的第三方型別（FMOD 的 StudioEventEmitter、HighlightEffect 之類）
        // 由專案端自己加進來，MonoFSM 只放 Unity 內建的。
        public static readonly List<string> VisualComponents = new()
        {
            "Renderer",
            "ParticleSystem",
            "AudioSource",
            "Light",
            "Cloth"
        };

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            PrefabTextCacheMarker.CacheWriter = WriteFor;
            PrefabTextCacheMarker.CachePathResolver = CachePathOf;
        }

        /// <summary>
        /// PrefabEdit 存檔後的 cache 更新。
        /// PrefabEdit 走 LoadPrefabContents 存檔，不會觸發 IBeforePrefabSaveCallbackReceiver，
        /// 所以 cache 得由它主動呼叫這裡，否則寫入 prefab 後 cache 就跟實際內容不同步。
        /// </summary>
        public static void RefreshCacheFor(string assetPath)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go == null) return;
            var marker = go.GetComponentInChildren<PrefabTextCacheMarker>(true);
            if (marker == null || !marker._cacheEnabled) return;
            Write(marker, go, assetPath);
        }

        [MenuItem("MonoFSM/Prefab Text Cache/重建全部")]
        public static void RebuildAll()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var written = 0;
            try
            {
                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "重建 Prefab Text Cache", $"{i + 1}/{guids.Length}  {path}",
                            (float)(i + 1) / guids.Length))
                        break;

                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null) continue;
                    var marker = go.GetComponentInChildren<PrefabTextCacheMarker>(true);
                    if (marker == null || !marker._cacheEnabled) continue;

                    if (Write(marker, go, path)) written++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[PrefabTextCache] 寫出 {written} 份到 {CacheRoot}/");
        }

        /// <summary>
        /// On-demand 精讀：從 prefab 的任一子樹當 root 匯出，不寫進 cache。
        /// cache 檔只是目錄（折疊行帶 (+N nodes) 成本），要看細節時呼叫這個現撈。
        /// </summary>
        /// <param name="assetPath">prefab asset path，例：Assets/0_Gameplay/0_Base/PPlayer.prefab</param>
        /// <param name="subPath">
        /// 子樹相對 root 的路徑，例：CharacterModules/Character FSM/[StateFolder] StateFolder。
        /// 留空 = 整棵。找不到時會列出該層有哪些子節點，方便下一次修正路徑。
        /// </param>
        /// <param name="depth">往下幾層；-1 = 不限</param>
        /// <param name="fullExpand">不摺疊已知子樹（StateFolder / VariableFolder …）</param>
        public static string ExportSubtree(
            string assetPath, string subPath = null, int depth = -1, bool fullExpand = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null) return $"# 找不到 prefab: {assetPath}";

            var root = asset.transform;
            if (!string.IsNullOrEmpty(subPath))
            {
                var found = root.Find(subPath);
                if (found == null) return DescribeChildren(root, subPath);
                root = found;
            }

            var options = fullExpand ? HierarchyExportOptions.FullExpand : HierarchyExportOptions.Default;
            options._maxDepth = depth;
            if (!fullExpand) options._excludeComponents.AddRange(VisualComponents);

            var sb = new StringBuilder();
            sb.AppendLine($"# prefab: res:{StripAssets(assetPath)}");
            if (!string.IsNullOrEmpty(subPath)) sb.AppendLine($"# subtree: {subPath}");
            sb.AppendLine();
            sb.Append(HierarchyTextExporter.Export(root.gameObject, options));
            return sb.ToString();
        }

        // 路徑打錯時，把該層實際有的子節點列出來，省一次來回
        private static string DescribeChildren(Transform root, string subPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# 找不到子樹: {subPath}");

            // 沿著路徑往下走到最後一個走得通的節點
            var cursor = root;
            var walked = "";
            foreach (var seg in subPath.Split('/'))
            {
                var next = cursor.Find(seg);
                if (next == null) break;
                cursor = next;
                walked = string.IsNullOrEmpty(walked) ? seg : $"{walked}/{seg}";
            }

            sb.AppendLine($"# 走到這裡為止: {(string.IsNullOrEmpty(walked) ? "(root)" : walked)}");
            sb.AppendLine("# 這層的子節點：");
            foreach (Transform child in cursor)
                sb.AppendLine($"  {child.name}  (+{CountDescendants(child)} nodes)");
            return sb.ToString();
        }

        private static int CountDescendants(Transform t)
        {
            var n = 0;
            foreach (Transform c in t) n += 1 + CountDescendants(c);
            return n;
        }

        // marker 的存檔 callback 入口
        public static void WriteFor(PrefabTextCacheMarker marker)
        {
            if (marker == null || !marker._cacheEnabled) return;

            var assetPath = AssetPathOf(marker);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning(
                    "[PrefabTextCache] 找不到對應的 prefab asset —— 只支援在 Prefab Mode 或直接對 prefab asset 操作",
                    marker);
                return;
            }

            Write(marker, RootOf(marker), assetPath);
        }

        public static string CachePathOf(PrefabTextCacheMarker marker)
        {
            var assetPath = AssetPathOf(marker);
            return string.IsNullOrEmpty(assetPath) ? null : ToCachePath(assetPath);
        }

        private static bool Write(PrefabTextCacheMarker marker, GameObject root, string assetPath)
        {
            if (root == null) return false;

            var options = marker._fullExpand
                ? HierarchyExportOptions.FullExpand
                : HierarchyExportOptions.Default;
            if (marker._expandPaths != null)
                options._expandPaths.AddRange(marker._expandPaths);
            if (marker._excludeVisual)
                options._excludeComponents.AddRange(VisualComponents);
            if (marker._foldInactive)
                options._includeInactive = false;
            if (marker._maxFieldCharsPerComponent > 0)
                options._maxFieldCharsPerComponent = marker._maxFieldCharsPerComponent;
            if (!marker._fullExpand)
                options._maxDepth = marker._maxDepth;

            var sb = new StringBuilder();
            // Prefab Mode 的 root 是暫時實例，Export 不會自己補 prefab 路徑，統一在這裡寫
            sb.AppendLine($"# prefab: res:{StripAssets(assetPath)}");
            sb.AppendLine("#");
            sb.AppendLine("# 這是目錄，不是全文。折疊行的 (+N nodes) 就是展開成本。");
            sb.AppendLine("# 要看某個子樹的細節，Unity 開著時用 uloop execute-dynamic-code 呼叫：");
            sb.AppendLine("#   MonoFSM.Editor.PrefabEditing.PrefabTextCacheWriter.ExportSubtree(");
            sb.AppendLine($"#       \"{assetPath}\", \"<子樹相對路徑>\")");
            sb.AppendLine("# 路徑打錯會回傳該層實際有哪些子節點，直接照著修就好。");
            sb.AppendLine("#");
            sb.AppendLine("# 要「改」這份 prefab，用同一套路徑語彙的 PrefabEdit：");
            sb.AppendLine("#   MonoFSM.Editor.PrefabEditing.PrefabEdit.AddNode / SetField / SetRef / DeleteNode");
            sb.AppendLine("# 存檔後這份 cache 會自動更新。");
            sb.AppendLine();
            sb.Append(HierarchyTextExporter.Export(root, options));

            if (marker._exportFsm)
            {
                var fsm = FsmTextExporter.Export(root);
                if (!string.IsNullOrEmpty(fsm) && !fsm.StartsWith("# (no FSM found"))
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                    sb.Append(fsm);
                }
            }

            var full = Path.Combine(RepoRoot(), ToCachePath(assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? ".");
            var text = sb.ToString();
            // 內容一樣就別碰檔案，免得在 git 裡製造無意義的 mtime / diff
            if (File.Exists(full) && File.ReadAllText(full) == text) return false;
            File.WriteAllText(full, text);
            return true;
        }

        private static GameObject RootOf(PrefabTextCacheMarker marker)
        {
            var stage = PrefabStageUtility.GetPrefabStage(marker.gameObject);
            return stage != null ? stage.prefabContentsRoot : marker.transform.root.gameObject;
        }

        private static string AssetPathOf(PrefabTextCacheMarker marker)
        {
            if (marker == null) return null;

            var stage = PrefabStageUtility.GetPrefabStage(marker.gameObject);
            if (stage != null) return stage.assetPath;

            var path = AssetDatabase.GetAssetPath(marker.gameObject);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        private static string ToCachePath(string assetPath)
        {
            var ext = Path.GetExtension(assetPath);
            var noExt = assetPath.Substring(0, assetPath.Length - ext.Length);
            return $"{CacheRoot}/{noExt}.md";
        }

        private static string StripAssets(string assetPath) =>
            assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;

        private static string RepoRoot() =>
            Directory.GetParent(Application.dataPath)!.FullName;
    }
}
