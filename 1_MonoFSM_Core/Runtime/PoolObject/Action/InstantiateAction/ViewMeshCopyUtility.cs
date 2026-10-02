using System.Collections.Generic;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 把一棵子樹的 MeshFilter + MeshRenderer（SkinnedMeshRenderer 取 sharedMesh 當靜態 mesh）複製成「純外觀」：
    /// 只有 MeshFilter / MeshRenderer，沒有 collider、MonoBehaviour、NetworkObject，保留相對 transform。
    /// 用途：排隊滑車外觀（CableCartQueueView）、掛貨滑車托盤上的商品外觀（GameDataViewRender）。
    /// 建外觀時才會配置記憶體，每幀不要叫。
    /// </summary>
    public static class ViewMeshCopyUtility
    {
        /// <summary>
        /// 複製單一顆 renderer 到 parent 底下。toRoot = 來源子樹 root 的 worldToLocalMatrix，
        /// 複本的 local pose = 它在來源 root 座標系裡的 pose。回傳 false = 沒有 mesh 可複製。
        /// </summary>
        public static bool CopyRenderer(Renderer src, Matrix4x4 toRoot, Transform parent, HideFlags flags)
        {
            Mesh mesh = null;
            if (src is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
            }
            else if (src is MeshRenderer)
            {
                var filter = src.GetComponent<MeshFilter>();
                if (filter != null)
                    mesh = filter.sharedMesh;
            }

            if (mesh == null)
                return false;

            var go = new GameObject(src.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.hideFlags = flags;
            go.layer = src.gameObject.layer;
            var t = go.transform;
            t.SetParent(parent, false);
            var m = toRoot * src.transform.localToWorldMatrix;
            t.localPosition = m.GetColumn(3);
            t.localRotation = m.rotation;
            t.localScale = m.lossyScale;

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var dst = go.GetComponent<MeshRenderer>();
            dst.sharedMaterials = src.sharedMaterials;
            dst.shadowCastingMode = src.shadowCastingMode;
            dst.receiveShadows = src.receiveShadows;
            return true;
        }

        /// <summary>
        /// 複製 sourceRoot 底下所有「自己到 sourceRoot 一路 activeSelf、renderer enabled」的 Mesh / Skinned renderer。
        /// 用 activeSelf 一路往上判斷而不是 activeInHierarchy：來源可能是 prefab asset（不在 scene 裡，activeInHierarchy 恆 false）。
        /// LODGroup 底下只取 LOD0，避免多層 LOD 疊在一起。buffer 由呼叫端提供（重複使用，不額外配置）。回傳複製了幾顆。
        /// </summary>
        public static int CopyActiveRenderers(Transform sourceRoot, Transform parent, HideFlags flags,
            List<Renderer> buffer)
        {
            if (sourceRoot == null || parent == null)
                return 0;
            buffer.Clear();
            sourceRoot.GetComponentsInChildren(true, buffer);
            var toRoot = sourceRoot.worldToLocalMatrix;
            var copied = 0;
            for (var i = 0; i < buffer.Count; i++)
            {
                var r = buffer[i];
                if (r == null || !r.enabled)
                    continue;
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                    continue;
                if (!IsActiveUpTo(r.transform, sourceRoot))
                    continue;
                if (!IsInLod0OrNoLod(r, sourceRoot))
                    continue;
                if (CopyRenderer(r, toRoot, parent, flags))
                    copied++;
            }

            buffer.Clear();
            return copied;
        }

        private static bool IsActiveUpTo(Transform t, Transform root)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                if (!cur.gameObject.activeSelf)
                    return false;
                if (cur == root)
                    return true;
            }

            return true;
        }

        //建外觀時才跑（GetLODs 會配置陣列），不在每幀路徑上
        private static bool IsInLod0OrNoLod(Renderer r, Transform root)
        {
            var lodGroup = r.GetComponentInParent<LODGroup>(true);
            if (lodGroup == null || !lodGroup.transform.IsChildOf(root))
                return true;
            var lods = lodGroup.GetLODs();
            if (lods.Length == 0)
                return true;
            var lod0 = lods[0].renderers;
            for (var i = 0; i < lod0.Length; i++)
                if (lod0[i] == r)
                    return true;
            //不在任何 LOD 裡的 renderer 照樣顯示
            for (var l = 1; l < lods.Length; l++)
            {
                var rs = lods[l].renderers;
                for (var i = 0; i < rs.Length; i++)
                    if (rs[i] == r)
                        return false;
            }

            return true;
        }

        /// <summary>
        /// 算 content 底下所有 MeshFilter 的 mesh bounds 在 space 座標系裡的 AABB。沒有 mesh 回 false。
        /// buffer 由呼叫端提供。
        /// </summary>
        public static bool TryGetLocalBounds(Transform content, Transform space, List<MeshFilter> buffer,
            out Bounds bounds)
        {
            bounds = default;
            buffer.Clear();
            content.GetComponentsInChildren(true, buffer);
            var has = false;
            var toSpace = space.worldToLocalMatrix;
            for (var i = 0; i < buffer.Count; i++)
            {
                var f = buffer[i];
                if (f == null || f.sharedMesh == null)
                    continue;
                var mb = f.sharedMesh.bounds;
                var m = toSpace * f.transform.localToWorldMatrix;
                var c = mb.center;
                var e = mb.extents;
                for (var k = 0; k < 8; k++)
                {
                    var corner = new Vector3(
                        c.x + ((k & 1) == 0 ? -e.x : e.x),
                        c.y + ((k & 2) == 0 ? -e.y : e.y),
                        c.z + ((k & 4) == 0 ? -e.z : e.z));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!has)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        has = true;
                    }
                    else
                    {
                        bounds.Encapsulate(p);
                    }
                }
            }

            buffer.Clear();
            return has;
        }
    }
}
