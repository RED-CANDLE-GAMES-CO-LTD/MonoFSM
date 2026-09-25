using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// `up prefab bounds`：prefab 內某節點底下 active 的 MeshRenderer / SkinnedMeshRenderer 的 AABB，
    /// 以及沿最長軸切段的「粗細分佈」（拿來判斷模型哪一頭朝哪邊，例如手電筒燈頭）。
    ///
    /// 為什麼走 Unity 端、不離線解析 FBX：importer 的軸向轉換（右手→左手的 X 反轉、Z-up
    /// 轉換、file scale、bakeAxisConversion）只有匯入後的 mesh 才是真值，離線讀 FBX 頂點
    /// 只能「推」模型朝向，證明不了。Editor 下 `Mesh.GetVertices` 不受 Read/Write 設定限制。
    ///
    /// 只讀：LoadPrefabContents → 算 → UnloadPrefabContents，不 dirty、不存檔。
    /// </summary>
    public static class EditBounds
    {
        private static readonly string[] AxisNames = { "X", "Y", "Z" };

        /// <param name="assetPath">prefab asset 路徑</param>
        /// <param name="nodePath">子樹路徑（留空 = root），解析規則同 read / peek</param>
        /// <param name="segments">沿合計 AABB 最長軸切幾段；0 = 不印分佈</param>
        public static string Bounds(string assetPath, string nodePath, int segments = 10)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                return $"# 找不到 prefab: {assetPath}";

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
                var rt = root.transform;
                var node = rt;
                if (!string.IsNullOrEmpty(nodePath))
                {
                    node = EditResolve.TryNodeExact(rt, nodePath, false, out var suggestion);
                    if (node == null)
                    {
                        var miss = "# " + EditResolve.DescribeChildren(rt, nodePath);
                        if (suggestion != null) miss += $"\n# 你可能想要 --node \"{suggestion}\"";
                        return miss;
                    }
                }

                return Compute(assetPath, rt, node, Math.Max(0, segments));
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# {abort.Message}";
            }
            catch (Exception e)
            {
                return $"# prefab bounds 失敗：{e.GetType().Name}: {e.Message}";
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string Compute(string assetPath, Transform rt, Transform node, int segments)
        {
            var sb = new StringBuilder();
            var nodeLabel = node == rt ? "(root)" : EditResolve.PathOf(rt, node);
            sb.AppendLine($"# prefab bounds: {assetPath}  node={nodeLabel}");
            sb.AppendLine("# 每行：renderer 路徑（相對 prefab root） | mesh | prefab root 座標系 AABB（m）");

            var toRoot = rt.worldToLocalMatrix;
            var toNode = node.worldToLocalMatrix;
            var rootPts = new List<Vector3>(4096);
            var verts = new List<Vector3>(4096);
            var totalRoot = new MinMax();
            var totalNode = new MinMax();
            int inactive = 0, off = 0, used = 0;

            foreach (var r in node.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh;
                if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
                else if (r is MeshRenderer)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    mesh = mf != null ? mf.sharedMesh : null;
                }
                else continue;

                if (!ActiveUnder(r.transform, node)) { inactive++; continue; }
                if (!r.enabled) { off++; continue; }

                var path = EditResolve.PathOf(rt, r.transform);
                if (string.IsNullOrEmpty(path)) path = "(root)";
                if (mesh == null)
                {
                    sb.AppendLine($"{path} | (沒有 mesh) | -");
                    continue;
                }

                verts.Clear();
                mesh.GetVertices(verts);
                if (verts.Count == 0)
                {
                    sb.AppendLine($"{path} | {MeshLabel(mesh)} | 讀不到頂點（isReadable={mesh.isReadable}）");
                    continue;
                }

                // SkinnedMeshRenderer 用 bind pose 的 sharedMesh 算，不是動畫當下的形狀
                var l2w = r.transform.localToWorldMatrix;
                var mRoot = toRoot * l2w;
                var mNode = toNode * l2w;
                var own = new MinMax();
                for (var i = 0; i < verts.Count; i++)
                {
                    var p = mRoot.MultiplyPoint3x4(verts[i]);
                    own.Add(p);
                    totalRoot.Add(p);
                    totalNode.Add(mNode.MultiplyPoint3x4(verts[i]));
                    rootPts.Add(p);
                }

                used++;
                sb.AppendLine($"{path} | {MeshLabel(mesh)} | {own.CenterSize()} n={verts.Count}" +
                              (r is SkinnedMeshRenderer ? " (skinned, bind pose)" : ""));
            }

            if (inactive > 0 || off > 0)
                sb.AppendLine($"# 略過：GameObject 關閉 {inactive} 個、renderer 關閉 {off} 個");

            if (used == 0)
            {
                sb.AppendLine("# 這個節點底下沒有可算的 active MeshRenderer / SkinnedMeshRenderer");
                return sb.ToString();
            }

            sb.AppendLine($"TOTAL root: {totalRoot.CenterSize()} min={V(totalRoot.Min)} max={V(totalRoot.Max)}");
            sb.AppendLine($"TOTAL local({nodeLabel}): {totalNode.CenterSize()} " +
                          $"min={V(totalNode.Min)} max={V(totalNode.Max)}");

            if (segments > 0) AppendSegments(sb, rootPts, totalRoot, segments);
            return sb.ToString();
        }

        /// <summary>沿 root 座標系合計 AABB 的最長軸切段，印每段頂點數與離中心軸的最大半徑。</summary>
        private static void AppendSegments(StringBuilder sb, List<Vector3> pts, MinMax box, int n)
        {
            var size = box.Max - box.Min;
            var center = (box.Max + box.Min) * 0.5f;
            var axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            int a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;
            var len = size[axis];
            var ax = AxisNames[axis];

            var count = new int[n];
            var radius = new float[n];
            foreach (var p in pts)
            {
                var b = len <= 0f ? 0 : (int)((p[axis] - box.Min[axis]) / len * n);
                if (b >= n) b = n - 1;
                if (b < 0) b = 0;
                float d1 = p[a1] - center[a1], d2 = p[a2] - center[a2];
                var r = Mathf.Sqrt(d1 * d1 + d2 * d2);
                count[b]++;
                if (r > radius[b]) radius[b] = r;
            }

            sb.AppendLine($"# segments：沿 root {ax} 軸切 {n} 段（#0 = -{ax} 端 → #{n - 1} = +{ax} 端），" +
                          $"半徑 = 離 AABB 中心軸（{AxisNames[a1]}{AxisNames[a2]} 平面）的最大距離");
            int thick = -1, thin = -1;
            for (var i = 0; i < n; i++)
            {
                var lo = box.Min[axis] + len * i / n;
                var hi = box.Min[axis] + len * (i + 1) / n;
                sb.AppendLine(count[i] == 0
                    ? $"#{i} {ax}[{F(lo)},{F(hi)}] n=0"
                    : $"#{i} {ax}[{F(lo)},{F(hi)}] n={count[i]} r={F(radius[i])}");
                if (count[i] == 0) continue;
                if (thick < 0 || radius[i] > radius[thick]) thick = i;
                if (thin < 0 || radius[i] < radius[thin]) thin = i;
            }

            string Half(int i) => i * 2 + 1 < n ? $"-{ax}" : i * 2 + 1 > n ? $"+{ax}" : "正中間";
            sb.AppendLine($"# 最粗段 #{thick}（{Half(thick)} 半邊）r={F(radius[thick])}；" +
                          $"最細段 #{thin}（{Half(thin)} 半邊）r={F(radius[thin])}");

            int first = -1, last = -1;
            for (var i = 0; i < n; i++) if (count[i] > 0) { first = i; break; }
            for (var i = n - 1; i >= 0; i--) if (count[i] > 0) { last = i; break; }
            if (first == last) return;
            var plusThicker = radius[last] > radius[first];
            sb.AppendLine($"# 兩端：-{ax} 端 r={F(radius[first])} / +{ax} 端 r={F(radius[last])} → " +
                          (Mathf.Approximately(radius[last], radius[first])
                              ? "兩端一樣粗"
                              : $"粗端在 {(plusThicker ? "+" : "-")}{ax}、細端在 {(plusThicker ? "-" : "+")}{ax}"));
        }

        /// <summary>從 t 往上走到 node（含）為止都 activeSelf 才算 active；node 上層的開關不算。</summary>
        private static bool ActiveUnder(Transform t, Transform node)
        {
            for (var c = t; c != null; c = c.parent)
            {
                if (!c.gameObject.activeSelf) return false;
                if (c == node) return true;
            }

            return true;
        }

        private static string MeshLabel(Mesh mesh)
        {
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) return $"(builtin) {mesh.name}";
            return path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ? path : $"{path}#{mesh.name}";
        }

        private static string F(float v) => v.ToString("F3", CultureInfo.InvariantCulture);
        private static string V(Vector3 v) => $"({F(v.x)},{F(v.y)},{F(v.z)})";

        private struct MinMax
        {
            public Vector3 Min, Max;
            private bool _any;

            public void Add(Vector3 p)
            {
                if (!_any)
                {
                    Min = Max = p;
                    _any = true;
                    return;
                }

                Min = Vector3.Min(Min, p);
                Max = Vector3.Max(Max, p);
            }

            public string CenterSize() => $"c={V((Min + Max) * 0.5f)} s={V(Max - Min)}";
        }
    }
}
