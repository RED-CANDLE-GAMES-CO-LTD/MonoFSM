using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Render
{
    public class RendererCollection : MonoBehaviour
    {
        public RendererCollection _rendererCollectionRef;

        [ShowInInspector] [AutoChildren] private Renderer[] _renderers;

        public Renderer[] Renderers => _rendererCollectionRef != null
            ? _rendererCollectionRef.Renderers
            : _renderers;

        //每個 renderer 的 material instance 陣列，lazy 建立後 cache，避免每幀 renderer.materials 的 alloc 與重複 instancing
        private Material[][] _cachedMaterials;

        public Material[][] CachedMaterials
        {
            get
            {
                if (_rendererCollectionRef != null)
                    return _rendererCollectionRef.CachedMaterials;

                if (_cachedMaterials == null)
                {
                    if (_renderers == null)
                        return null;
                    _cachedMaterials = new Material[_renderers.Length][];
                    for (var i = 0; i < _renderers.Length; i++)
                        _cachedMaterials[i] = _renderers[i] != null ? _renderers[i].materials : null;
                }

                return _cachedMaterials;
            }
        }

        public void SetRenderingLayerMask(uint mask)
        {
            if (_rendererCollectionRef != null)
            {
                _rendererCollectionRef.SetRenderingLayerMask(mask);
                return;
            }

            if (_renderers == null || _renderers.Length == 0)
                return;
            foreach (var r in _renderers)
            {
                r.renderingLayerMask = mask;
            }
        }
    }
}
