using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 「這個節點被誰指到」「它又指向誰」的引用反查。
    ///
    /// 為什麼走 Unity 而不是離線索引：離線 YAML 的 refs 表只收**本檔直接寫出**的引用邊，
    /// 而這個專案大量的引用是 prefab override（寫在 m_Modifications 裡），refs 表 0 命中；
    /// override 的目標雖然在 mods 表裡，但被格式化成字串塞進 value 欄位、無索引，
    /// 且拿到的是裸 fileID —— 要翻成可讀路徑又會撞上 variant 階層斷裂。
    /// SerializedObject 看到的是**合併後的真值**，一趟就能回「節點路徑 + 欄位名」。
    ///
    /// 範圍限「同一顆 prefab / 當前 scene 之內」。跨資產的全庫粗查是離線索引的活。
    /// </summary>
    public static class EditRefs
    {
        /// <summary>Unity 內建的結構性引用 —— 每個節點都有，反查時是純雜訊。</summary>
        private static readonly HashSet<string> StructuralProps = new()
        {
            "m_GameObject",
            "m_Father",
            "m_Children",
            "m_Script",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
        };

        /// <param name="assetPath">prefab asset path</param>
        /// <param name="nodePath">目標節點相對 root 的路徑；留空 = root 自己</param>
        /// <param name="componentType">只看目標節點上的這個 component；留空 = 節點與其所有 component</param>
        /// <param name="outbound">false = 誰指向目標（預設）；true = 目標指向誰</param>
        public static string PrefabRefs(
            string assetPath, string nodePath, string componentType = null,
            bool outbound = false, int limit = 60)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                return Report(
                    $"prefab: {assetPath}", root.transform,
                    () => EditResolve.Node(root.transform, nodePath),
                    nodePath, componentType, outbound, limit);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>當前開著的 scene 版。nodePath 第一段是 root object 名稱。</summary>
        public static string SceneRefs(
            string nodePath, string componentType = null, bool outbound = false, int limit = 60)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            return Report(
                $"scene: {scene.name}", null,
                () => EditResolve.NodeInRoots(roots.ToList(), nodePath),
                nodePath, componentType, outbound, limit,
                roots.Select(g => g.transform).ToList());
        }

        private static string Report(
            string header, Transform searchRoot,
            System.Func<Transform> resolveTarget,
            string nodePath, string componentType, bool outbound, int limit,
            IList<Transform> extraRoots = null)
        {
            Transform node;
            Component onlyComp = null;
            try
            {
                node = resolveTarget();
                if (!string.IsNullOrEmpty(componentType))
                    onlyComp = EditResolve.Comp(node, nodePath, componentType);
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# {abort.Message}";
            }

            var scope = searchRoot != null
                ? new List<Transform> { searchRoot }
                : extraRoots ?? new List<Transform>();

            var sb = new StringBuilder($"# {header}\n");
            return outbound
                ? Outbound(sb, node, onlyComp, searchRoot, scope, limit)
                : Inbound(sb, node, onlyComp, searchRoot, scope, limit);
        }

        // ---- 誰指向我 ----

        private static string Inbound(
            StringBuilder sb, Transform node, Component onlyComp,
            Transform searchRoot, IList<Transform> scope, int limit)
        {
            // 目標集合：指定了 component 就只有它，否則節點自己 + 節點上所有 component。
            // 兩者都要收 —— MonoFSM 多數欄位指 component，但 _target / _colliderRoot 這類指 Transform。
            var targets = new HashSet<Object>();
            if (onlyComp != null)
            {
                targets.Add(onlyComp);
            }
            else
            {
                targets.Add(node.gameObject);
                foreach (var c in node.GetComponents<Component>())
                    if (c != null) targets.Add(c);
            }

            var hits = new List<string>();
            var truncated = false;
            foreach (var owner in scope.SelectMany(Walk))
            {
                if (truncated) break;
                foreach (var comp in owner.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var prop = so.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var value = prop.objectReferenceValue;
                        if (value == null || !targets.Contains(value)) continue;
                        if (StructuralProps.Contains(Leaf(prop.propertyPath))) continue;

                        if (hits.Count >= limit)
                        {
                            truncated = true;
                            break;
                        }

                        hits.Add(
                            $"  {PathOf(owner, searchRoot)}{NoteText.Suffix(NoteText.Of(comp))}\n" +
                            $"      {comp.GetType().Name}.{prop.propertyPath}" +
                            $"{TargetSuffix(value, onlyComp)}");
                    }

                    if (truncated) break;
                }
            }

            sb.AppendLine(
                $"{hits.Count}{(truncated ? "+" : "")} 個引用指向 " +
                $"{PathOf(node, searchRoot)}{(onlyComp != null ? $".{onlyComp.GetType().Name}" : "")}");
            foreach (var h in hits) sb.AppendLine(h);
            if (truncated) sb.AppendLine($"  # 到 limit {limit} 就停了，還有更多");
            if (hits.Count == 0)
                sb.AppendLine("  # 這個範圍內沒有引用。跨資產的引用要用離線索引查（up find）");
            return sb.ToString();
        }

        /// <summary>沒指定 component 時，標出命中的是節點上的哪一個。</summary>
        private static string TargetSuffix(Object value, Component onlyComp)
        {
            if (onlyComp != null) return "";
            if (value is GameObject) return "  → (GameObject)";
            return value is Component c ? $"  → {c.GetType().Name}" : "";
        }

        // ---- 我指向誰 ----

        private static string Outbound(
            StringBuilder sb, Transform node, Component onlyComp,
            Transform searchRoot, IList<Transform> scope, int limit)
        {
            var comps = onlyComp != null
                ? new[] { onlyComp }
                : node.GetComponents<Component>().Where(c => c != null).ToArray();

            var count = 0;
            sb.AppendLine($"{PathOf(node, searchRoot)} 指向：");
            foreach (var comp in comps)
            {
                var lines = new List<string>();
                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                while (prop.NextVisible(true) && count < limit)
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = prop.objectReferenceValue;
                    if (value == null) continue;
                    if (StructuralProps.Contains(Leaf(prop.propertyPath))) continue;

                    lines.Add($"      {prop.propertyPath} -> {Describe(value, searchRoot, scope)}");
                    count++;
                }

                if (lines.Count == 0) continue;
                sb.AppendLine($"  <{comp.GetType().Name}>");
                foreach (var l in lines) sb.AppendLine(l);
            }

            if (count == 0) sb.AppendLine("  # 沒有非空的物件引用");
            else if (count >= limit) sb.AppendLine($"  # 到 limit {limit} 就停了，還有更多");
            return sb.ToString();
        }

        /// <summary>引用目標：同一棵樹裡就印節點路徑，否則印資產路徑。</summary>
        private static string Describe(Object value, Transform searchRoot, IList<Transform> scope)
        {
            var t = value switch
            {
                GameObject go => go.transform,
                Component c => c.transform,
                _ => null,
            };

            if (t != null && scope.Any(r => t == r || t.IsChildOf(r)))
                return $"{PathOf(t, searchRoot)}" +
                       (value is Component c2 ? $"#{c2.GetType().Name}" : "") +
                       NoteText.Suffix(NoteOf(value, t));

            var asset = AssetDatabase.GetAssetPath(value);
            return string.IsNullOrEmpty(asset)
                ? $"{value.name} <{value.GetType().Name}> (樹外)"
                : $"res:{asset}" + (value is Component c3 ? $"#{c3.GetType().Name}" : "") +
                  NoteText.Suffix(NoteText.Of(value));
        }

        /// <summary>
        /// 引用目標的 note。目標常常是 Transform（`_target` 這類欄位），note 卻寫在同節點的
        /// 別的 component 上（例如 MonoStateBehaviour），所以自己沒有就退回節點層級找。
        /// </summary>
        private static string NoteOf(Object value, Transform node)
        {
            var own = NoteText.Of(value);
            if (!string.IsNullOrEmpty(own)) return own;
            return node != null ? NoteText.OfGameObject(node.gameObject) : "";
        }

        // ---- 共用 ----

        private static IEnumerable<Transform> Walk(Transform t)
        {
            yield return t;
            foreach (Transform child in t)
            foreach (var d in Walk(child))
                yield return d;
        }

        /// <summary>searchRoot 為 null（scene）時走到 root object 為止，路徑含 root 名稱。</summary>
        private static string PathOf(Transform node, Transform searchRoot)
        {
            if (node == searchRoot) return ".";
            var parts = new List<string>();
            for (var t = node; t != null && t != searchRoot; t = t.parent)
                parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Leaf(string propertyPath)
        {
            var dot = propertyPath.LastIndexOf('.');
            return dot < 0 ? propertyPath : propertyPath.Substring(dot + 1);
        }
    }
}
