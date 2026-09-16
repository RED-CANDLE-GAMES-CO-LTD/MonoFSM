using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 跨 prefab 複製子樹（`copyfrom` op）。
    ///
    /// 為什麼需要：`mv` 只能在同一份 prefab 內搬節點。把一支 prefab 拆成 base + variant 時，
    /// 得把「變體專屬」的子樹從舊檔（快照）搬進新 variant，兩邊是不同的 prefab contents，
    /// 沒有任何現成 op 做得到。
    ///
    /// 三個非做不可的細節（每一個都是實測踩出來的，不要簡化掉）：
    ///
    /// 1. **`Object.Instantiate` 會把 nested prefab 連結扯斷。** 2026-09-15 實測：
    ///    對 prefab contents 裡的 nested 實例 Instantiate，複製出來的是一坨普通 GameObject，
    ///    `up prefab read` 不再有 `(prefab:res:…)` 後綴，override 全部變成本體資料。
    ///    所以複製完要把每個「原本是 prefab 實例 root」的節點就地換成
    ///    `PrefabUtility.InstantiatePrefab` + `SetPropertyModifications` 重建的真實例。
    ///
    /// 2. **引用要重映射。** 指向子樹外的欄位（`_viewRoot`、`_bindingRoot`、`_rb`）複製後
    ///    仍指著來源 contents 的物件，存檔會變 null；指向子樹內的欄位則因為第 1 點的
    ///    就地替換而失效。統一在最後掃一次 SerializedObject 修掉：子樹內走 src→copy 對照表，
    ///    子樹外走「hierarchy 相對路徑到目的 prefab 找同路徑節點」。
    ///
    /// 3. **子樹之間會互指**（拆件模組 ↔ 細胞部件），不管先搬哪一棵都會有一邊指不到。
    ///    所以對不到的引用不是當場放棄，而是排進 pending，等整批 ops 跑完再解一次
    ///    （`FlushPending`），仍解不掉的才印出來要人手補。
    /// </summary>
    internal static class EditCopy
    {
        // 同一批 ops 常常從同一份快照搬好幾棵子樹，contents 載一次就好。
        private static readonly Dictionary<string, GameObject> Sources = new();

        /// <summary>整批 ops 跑完才解的引用（子樹互指時第一輪必然解不到）。</summary>
        private sealed class Pending
        {
            public Component Comp;
            public string PropertyPath;
            public string SrcRelPath; // 相對 src root
            public UnityEngine.Object SrcVal;
            public Transform SrcOwner;
            public string Label;
        }

        private static readonly List<Pending> Deferred = new();

        private static Transform SrcRoot(string srcPath)
        {
            if (Sources.TryGetValue(srcPath, out var cached) && cached != null)
                return cached.transform;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(srcPath) == null)
                throw new Abort($"copyfrom 找不到來源 prefab: {srcPath}");

            var root = PrefabUtility.LoadPrefabContents(srcPath);
            if (root == null) throw new Abort($"copyfrom 無法載入來源 prefab contents: {srcPath}");
            Sources[srcPath] = root;
            return root.transform;
        }

        /// <summary>
        /// 卸載這批 ops 載入過的所有來源 contents。
        /// **一定要在目的 prefab 存檔前呼叫**（而且要排在 FlushPending 之後）——
        /// 否則殘留的跨 contents 引用會被序列化成指向另一個 preview scene 的壞引用。
        /// </summary>
        internal static void UnloadAll()
        {
            foreach (var kv in Sources)
                if (kv.Value != null)
                    PrefabUtility.UnloadPrefabContents(kv.Value);
            Sources.Clear();
            Deferred.Clear();
        }

        /// <summary>
        /// `copyfrom|&lt;srcPrefabPath&gt;|&lt;srcNode&gt;|&lt;dstParent&gt;[|&lt;newName&gt;]`
        /// </summary>
        internal static string CopyFrom(
            Transform dstRoot, string srcPath, string srcNodePath, string dstParentPath,
            string newName, out string fullPath)
        {
            var srcRoot = SrcRoot(srcPath);
            var srcNode = EditResolve.Node(srcRoot, srcNodePath);
            if (srcNode == srcRoot)
                throw new Abort("copyfrom 不能複製來源 prefab 的 root，請指定子節點");

            var dstParent = EditResolve.Node(dstRoot, dstParentPath);
            if (dstParent.IsChildOf(dstRoot) == false && dstParent != dstRoot)
                throw new Abort("copyfrom 的 dstParent 不在目的 prefab 裡");

            var nodeName = string.IsNullOrEmpty(newName) ? srcNode.name : newName;
            fullPath = string.IsNullOrEmpty(dstParentPath) ? nodeName : $"{dstParentPath}/{nodeName}";
            if (dstParent.Find(nodeName) != null)
                return $"（跳過）{EditResolve.Describe(dstParentPath)}/{nodeName} 已存在";

            // 1) 先整棵 Instantiate：plain 節點的 component 值、階層順序都由 Unity 處理好。
            var copy = UnityEngine.Object.Instantiate(srcNode.gameObject);
            copy.name = nodeName;
            copy.transform.SetParent(dstParent, false);
            CopyLocalTransform(srcNode, copy.transform);
            copy.SetActive(srcNode.gameObject.activeSelf);

            // 2) 把每個「來源是 prefab 實例 root」的節點就地換成真的 nested 實例
            var warnings = new List<string>();
            var copyTr = copy.transform;
            var rebuilt = 0;
            // 子樹 root 本身就是 nested 實例時會被就地換掉（原本那顆 GameObject 被銷毀），
            // 後面所有步驟都要改用新的 transform，否則整批噴 MissingReferenceException。
            if (PrefabUtility.IsAnyPrefabInstanceRoot(srcNode.gameObject))
            {
                var replaced = Rebuild(srcNode, copyTr, warnings);
                if (replaced != null)
                {
                    copyTr = replaced;
                    rebuilt++;
                }
            }
            else
            {
                rebuilt = RebuildNestedInstances(srcNode, copyTr, warnings);
            }

            // 3) src -> copy 的物件對照表（第 2 步換過之後才建，才對得到新實例上的 component）
            var map = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            BuildMap(srcNode, copyTr, map, warnings);

            // 4) 重映射所有物件引用
            var fixedCount = Remap(srcRoot, dstRoot, copyTr, map, warnings);

            var sb = new StringBuilder();
            sb.Append($"複製 {srcPath}::{srcNodePath} -> {fullPath}" +
                      $"（含 {EditResolve.CountDescendants(copyTr)} 個子節點" +
                      $"，重建 nested 實例 {rebuilt} 個，引用重映射 {fixedCount} 條" +
                      $"，待解 {Deferred.Count} 條）");
            foreach (var w in warnings) sb.Append("\n# copyfrom: " + w);
            return sb.ToString();
        }

        private static void CopyLocalTransform(Transform src, Transform dst)
        {
            dst.localPosition = src.localPosition;
            dst.localRotation = src.localRotation;
            dst.localScale = src.localScale;
        }

        // ---------- 2) nested 實例重建 ----------

        private static int RebuildNestedInstances(
            Transform srcNode, Transform copyRoot, List<string> warnings)
        {
            var count = 0;
            // 由外而內、由上而下；替換後不再往被替換的子樹裡鑽（內層由 asset 自己帶）。
            // root 自己的替換由呼叫端處理（會換掉 transform），這裡只從 children 開始。
            var queue = new Queue<(Transform src, Transform dst)>();
            var rootN = Mathf.Min(srcNode.childCount, copyRoot.childCount);
            for (var i = 0; i < rootN; i++)
                queue.Enqueue((srcNode.GetChild(i), copyRoot.GetChild(i)));
            while (queue.Count > 0)
            {
                var (src, dst) = queue.Dequeue();
                if (dst == null) continue;

                if (PrefabUtility.IsAnyPrefabInstanceRoot(src.gameObject))
                {
                    if (Rebuild(src, dst, warnings) != null) count++;
                    continue; // 內層 nested 由 asset 自己帶，不再遞迴
                }

                var n = Mathf.Min(src.childCount, dst.childCount);
                if (src.childCount != dst.childCount)
                    warnings.Add($"結構對不上：{EditResolve.PathOf(copyRoot, dst)} 子節點數 " +
                                 $"{dst.childCount} != 來源 {src.childCount}");
                for (var i = 0; i < n; i++) queue.Enqueue((src.GetChild(i), dst.GetChild(i)));
            }

            return count;
        }

        private static Transform Rebuild(Transform src, Transform dst, List<string> warnings)
        {
            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(src.gameObject);
            var asset = string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                warnings.Add($"'{src.name}' 是 prefab 實例但找不到來源 asset，" +
                             "維持成普通節點（nested 連結已斷）");
                return null;
            }

            var parent = dst.parent;
            var index = dst.GetSiblingIndex();
            var name = dst.name;

            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (inst == null)
            {
                warnings.Add($"'{src.name}' 無法重建 nested 實例（{assetPath}）");
                return null;
            }

            UnityEngine.Object.DestroyImmediate(dst.gameObject);

            inst.transform.SetParent(parent, false);
            inst.transform.SetSiblingIndex(index);
            // m_Name / m_IsActive / TRS 都可能在 modifications 裡，先套再蓋回來源的實際值
            var mods = PrefabUtility.GetPropertyModifications(src.gameObject);
            if (mods != null) PrefabUtility.SetPropertyModifications(inst, mods);
            inst.name = name;
            CopyLocalTransform(src, inst.transform);
            inst.SetActive(src.gameObject.activeSelf);

            // 加在實例上的物件 / component 不在 PropertyModifications 裡，會遺失，明確報出來
            try
            {
                var addedGo = PrefabUtility.GetAddedGameObjects(src.gameObject);
                if (addedGo != null && addedGo.Count > 0)
                    warnings.Add($"'{src.name}' 上有 {addedGo.Count} 個 added GameObject" +
                                 "（實例額外加的節點），copyfrom 不會複製，請手動補");
                var addedComp = PrefabUtility.GetAddedComponents(src.gameObject);
                if (addedComp != null && addedComp.Count > 0)
                    warnings.Add($"'{src.name}' 上有 {addedComp.Count} 個 added Component，" +
                                 "copyfrom 不會複製，請手動補");
                var removed = PrefabUtility.GetRemovedComponents(src.gameObject);
                if (removed != null && removed.Count > 0)
                    warnings.Add($"'{src.name}' 上有 {removed.Count} 個 removed Component，" +
                                 "重建後會變回存在，請手動移除");
            }
            catch (Exception e)
            {
                warnings.Add($"'{src.name}' override 盤點失敗：{e.GetType().Name}");
            }

            return inst.transform;
        }

        // ---------- 3) src -> copy 對照表 ----------

        private static void BuildMap(
            Transform src, Transform dst,
            Dictionary<UnityEngine.Object, UnityEngine.Object> map, List<string> warnings)
        {
            map[src.gameObject] = dst.gameObject;
            MapComponents(src, dst, map, warnings);

            var n = Mathf.Min(src.childCount, dst.childCount);
            for (var i = 0; i < n; i++) BuildMap(src.GetChild(i), dst.GetChild(i), map, warnings);
        }

        private static void MapComponents(
            Transform src, Transform dst,
            Dictionary<UnityEngine.Object, UnityEngine.Object> map, List<string> warnings)
        {
            var srcComps = src.GetComponents<Component>();
            foreach (var group in srcComps.Where(c => c != null).GroupBy(c => c.GetType()))
            {
                var dstSame = dst.GetComponents(group.Key);
                var i = 0;
                foreach (var sc in group)
                {
                    if (i < dstSame.Length) map[sc] = dstSame[i];
                    else
                        warnings.Add($"{EditResolve.PathOf(null, dst) ?? dst.name} 上少了 " +
                                     $"{group.Key.Name}（來源有 {group.Count()} 顆）");
                    i++;
                }
            }
        }

        // ---------- 4) 引用重映射 ----------

        // Unity 內建的 prefab / 階層連結欄位，碰了會壞掉；一律不動。
        private static readonly HashSet<string> Forbidden = new()
        {
            "m_PrefabInstance", "m_PrefabAsset", "m_CorrespondingSourceObject",
            "m_GameObject", "m_Father", "m_Children",
        };

        private static int Remap(
            Transform srcRoot, Transform dstRoot, Transform copyRoot,
            Dictionary<UnityEngine.Object, UnityEngine.Object> map, List<string> warnings)
        {
            var fixedCount = 0;
            foreach (var comp in copyRoot.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                if (comp is Transform) continue; // 只有 m_Father / m_Children，不該動

                SerializedObject so;
                try { so = new SerializedObject(comp); }
                catch (Exception) { continue; }

                var changed = false;
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (Forbidden.Contains(it.name)) continue;

                    var val = it.objectReferenceValue;
                    if (val == null) continue;

                    // 子樹內：走對照表（第 2 步換掉 nested 實例後，Instantiate 的內部 remap 已失效）
                    if (map.TryGetValue(val, out var mapped))
                    {
                        if (mapped != val)
                        {
                            it.objectReferenceValue = mapped;
                            changed = true;
                            fixedCount++;
                        }

                        continue;
                    }

                    var owner = OwnerTransform(val);
                    // 不是來源 contents 的場景物件（asset / 材質 / SO / prefab asset）：不用動
                    if (owner == null || !owner.IsChildOf(srcRoot)) continue;

                    var rel = EditResolve.PathOf(srcRoot, owner);
                    var label = $"{EditResolve.PathOf(dstRoot, comp.transform)}" +
                                $".{comp.GetType().Name}.{it.propertyPath} -> " +
                                $"{(string.IsNullOrEmpty(rel) ? "(src root)" : rel)}";

                    if (TryExternal(dstRoot, rel, val, owner, out var replacement))
                    {
                        it.objectReferenceValue = replacement;
                        changed = true;
                        fixedCount++;
                        continue;
                    }

                    // 兩棵子樹互指時第一輪必然解不到，排進 pending 等整批跑完再試
                    Deferred.Add(new Pending
                    {
                        Comp = comp, PropertyPath = it.propertyPath, SrcRelPath = rel,
                        SrcVal = val, SrcOwner = owner, Label = label,
                    });
                }

                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }

            return fixedCount;
        }

        private static bool TryExternal(
            Transform dstRoot, string rel, UnityEngine.Object srcVal, Transform srcOwner,
            out UnityEngine.Object replacement)
        {
            replacement = null;
            var target = string.IsNullOrEmpty(rel) ? dstRoot : EditResolve.TryNode(dstRoot, rel);
            if (target == null) return false;
            replacement = MatchOn(target, srcVal, srcOwner);
            return replacement != null;
        }

        /// <summary>
        /// 整批 ops 跑完後再解一次 pending 引用（子樹互指用）。
        /// **要排在 UnloadAll 之前**，否則來源物件已經沒了，連路徑都算不出來。
        /// </summary>
        internal static string FlushPending(Transform dstRoot)
        {
            if (Deferred.Count == 0) return "";

            var ok = 0;
            var left = new List<string>();
            foreach (var p in Deferred)
            {
                if (p.Comp == null)
                {
                    left.Add(p.Label + "（節點被後續操作刪掉了）");
                    continue;
                }

                if (!TryExternal(dstRoot, p.SrcRelPath, p.SrcVal, p.SrcOwner, out var replacement))
                {
                    left.Add(p.Label + "（目的 prefab 找不到對應節點）");
                    continue;
                }

                var so = new SerializedObject(p.Comp);
                var prop = so.FindProperty(p.PropertyPath);
                if (prop == null)
                {
                    left.Add(p.Label + "（propertyPath 已不存在）");
                    continue;
                }

                prop.objectReferenceValue = replacement;
                so.ApplyModifiedPropertiesWithoutUndo();
                ok++;
            }

            Deferred.Clear();
            var sb = new StringBuilder($"# copyfrom 延後解引用：{ok} 條 OK");
            if (left.Count > 0)
            {
                sb.Append($"，{left.Count} 條解不掉（請用 `ref` 手補或確認不需要）：");
                foreach (var l in left) sb.Append("\n#   " + l);
            }

            sb.Append('\n');
            return sb.ToString();
        }

        private static Transform OwnerTransform(UnityEngine.Object obj) =>
            obj switch
            {
                GameObject go => go.transform,
                Component c => c == null ? null : c.transform,
                _ => null,
            };

        /// <summary>在目的節點上找「同型別、同順位」的對應物件。</summary>
        private static UnityEngine.Object MatchOn(
            Transform target, UnityEngine.Object srcVal, Transform srcOwner)
        {
            if (srcVal is GameObject) return target.gameObject;

            var type = srcVal.GetType();
            var srcSame = srcOwner.GetComponents(type);
            var index = Array.IndexOf(srcSame, srcVal);
            var dstSame = target.GetComponents(type);
            if (dstSame.Length == 0) return null;
            if (index < 0 || index >= dstSame.Length) return dstSame[0];
            return dstSame[index];
        }
    }
}
