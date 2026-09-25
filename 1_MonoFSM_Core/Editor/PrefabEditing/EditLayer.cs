using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoFSM.Core.Detection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// GameObject layer 的解析、顯示與慣例檢查 —— uprefab 的 `layer|` 操作、read / peek / locate
    /// 的 layer 標記、do 存檔後的 Detector layer 檢查共用這一份。
    ///
    /// 慣例：`RequiresDetectorLayer` 為 true 的偵測器（目前是 TriggerDetectorSource 與子類）必須放在
    /// <see cref="DetectorLayer"/>。collision matrix 照 Detector layer 配，放錯 layer 的 trigger 會靜默打不到
    /// （或多打），Inspector 上看起來完全正常。cast / overlap 打什麼看 query mask，不檢查。
    /// layer 名字與 index 只認 DetectorLayer 這一個定義點；專案沒有那個 layer 時整條檢查自動停用。
    /// </summary>
    internal static class EditLayer
    {
        internal const string DetectorLayerName = DetectorLayer.Name;

        /// <summary>layer 名字（或 0..31 的數字）→ index；打錯就列出所有有名字的 layer。</summary>
        internal static int Resolve(string name, string verb)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                throw new Abort($"`{verb}` 要給 layer 名字。可用的：{Available()}");
            var idx = LayerMask.NameToLayer(trimmed);
            if (idx >= 0) return idx;
            if (int.TryParse(trimmed, out var n) && n >= 0 && n < 32 &&
                !string.IsNullOrEmpty(LayerMask.LayerToName(n)))
                return n;
            for (var i = 0; i < 32; i++)
            {
                var n2 = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(n2) && string.Equals(n2, trimmed, System.StringComparison.OrdinalIgnoreCase))
                    throw new Abort($"`{verb}` 不認得 layer '{trimmed}'：layer 名字大小寫要一致，你是不是要 '{n2}'？");
            }

            throw new Abort($"`{verb}` 不認得 layer '{trimmed}'。可用的：{Available()}");
        }

        /// <summary>所有有名字的 layer，`名字(index)` 逗號分隔。</summary>
        internal static string Available()
        {
            var names = new List<string>();
            for (var i = 0; i < 32; i++)
            {
                var n = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(n)) names.Add($"{n}({i})");
            }

            return string.Join(", ", names);
        }

        internal static string Name(int layer)
        {
            var n = LayerMask.LayerToName(layer);
            return string.IsNullOrEmpty(n) ? layer.ToString() : n;
        }

        /// <summary>
        /// 節點行要附的 layer 標記：Default 且沒違規回 null（省輸出）；
        /// 違反 Detector 慣例時一律印（連 Default 也印出來，才看得出錯在哪）。
        /// </summary>
        internal static string NodeTag(GameObject go)
        {
            if (go == null) return null;
            var violates = ViolatesDetectorConvention(go);
            if (go.layer == 0 && !violates) return null;
            return violates ? $"layer={Name(go.layer)} ⚠應在{DetectorLayerName}" : $"layer={Name(go.layer)}";
        }

        internal static bool ViolatesDetectorConvention(GameObject go)
        {
            if (!DetectorLayer.Exists || go.layer == DetectorLayer.Index) return false;
            foreach (var src in go.GetComponents<AbstractDetectionSource>())
                if (src != null && src.RequiresDetectorLayer) return true;
            return false;
        }

        /// <summary>子樹（含 inactive）裡違反 Detector 慣例的節點，依 hierarchy 順序、不重複。</summary>
        internal static List<Transform> FindViolations(Transform root)
        {
            var result = new List<Transform>();
            if (root == null || !DetectorLayer.Exists) return result;
            var seen = new HashSet<GameObject>();
            foreach (var src in root.GetComponentsInChildren<AbstractDetectionSource>(true))
            {
                if (src == null || !seen.Add(src.gameObject)) continue;
                if (ViolatesDetectorConvention(src.gameObject)) result.Add(src.transform);
            }

            return result;
        }

        /// <summary>子樹裡違規節點的 root-relative 路徑集合（do 用來比「這批新造成的」）。</summary>
        internal static HashSet<string> ViolationPaths(Transform prefabRoot) =>
            new(FindViolations(prefabRoot).Select(t => EditResolve.PathOf(prefabRoot, t) ?? t.name));

        /// <summary>
        /// 修正指令。prefabAssetPath 給了就當 prefab（do 時手上是 loaded contents，拿不到 asset path）；
        /// 否則依物件本身判斷是 prefab asset 還是 scene 物件。
        /// </summary>
        internal static string FixCommand(Transform t, string prefabAssetPath = null)
        {
            var assetPath = prefabAssetPath ?? AssetDatabase.GetAssetPath(t.gameObject);
            if (!string.IsNullOrEmpty(assetPath))
                return $"up prefab do \"{assetPath}\" \"layer|{EditResolve.PathOf(t.root, t)}|{DetectorLayerName}\"";

            var scene = t.gameObject.scene;
            if (scene.IsValid() && !EditorSceneManager.IsPreviewScene(scene))
            {
                var rel = EditResolve.PathOf(t.root, t);
                var full = EditResolve.EscapeName(t.root.name) + (string.IsNullOrEmpty(rel) ? "" : "/" + rel);
                return $"up scene do \"layer|{full}|{DetectorLayerName}\" \"save\"";
            }

            return $"layer|{EditResolve.PathOf(t.root, t)}|{DetectorLayerName}";
        }

        /// <summary>
        /// 給 read 的 header：子樹裡有違規節點就回一段警告（含最多 cap 條修正指令），沒有回 null。
        /// </summary>
        internal static string ViolationReport(Transform scanRoot, string prefabAssetPath = null, int cap = 6)
        {
            var list = FindViolations(scanRoot);
            if (list.Count == 0) return null;
            return FormatWarning(list, list.Count, prefabAssetPath, cap, "");
        }

        internal static string FormatWarning(
            IList<Transform> shown, int total, string prefabAssetPath, int cap, string scopeNote)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# ⚠ layer 慣例：{total} 顆 TriggerDetectorSource 不在 {DetectorLayerName} layer{scopeNote}" +
                          "（collision matrix 照 Detector 配，放錯可能靜默打不到）。修正：");
            foreach (var t in shown.Take(cap))
                sb.AppendLine("#   " + FixCommand(t, prefabAssetPath));
            if (total > cap) sb.AppendLine($"#   …另外 {total - cap} 顆");
            return sb.ToString();
        }

        /// <summary>
        /// 設 layer，並讓它在 prefab instance / variant 上成為 override。
        /// children = true 時連整棵子樹一起設（跟 Unity Inspector 改 layer 時問「Yes, change children」同義）。
        /// </summary>
        internal static List<GameObject> Apply(Transform node, int layer, bool children)
        {
            var targets = children
                ? node.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToList()
                : new List<GameObject> { node.gameObject };
            foreach (var go in targets)
            {
                go.layer = layer;
                EditorUtility.SetDirty(go);
                // 跟 active 同一個道理：直接寫 property 不會自己記 override，nested / variant 上存檔後會消失
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            }

            return targets;
        }

        /// <summary>`layer|<node>|<name>|children` 第三格的判斷：children / true / 1 / recursive 都算。</summary>
        internal static bool IsChildrenFlag(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var v = s.Trim().ToLowerInvariant();
            return v is "children" or "true" or "1" or "recursive";
        }
    }
}
