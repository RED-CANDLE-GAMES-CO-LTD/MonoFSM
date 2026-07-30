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
                    var result = new Material[_renderers.Length][];
                    var allReady = true;
                    for (var i = 0; i < _renderers.Length; i++)
                    {
                        var r = _renderers[i];
                        result[i] = r != null ? r.materials : null;
                        //renderer 還沒 ready 時 materials 會回長度 0，這種結果不能 cache，
                        //否則之後永遠拿不到真的 material instance
                        if (r != null && (result[i] == null || result[i].Length == 0))
                            allReady = false;
                    }

                    //還沒 ready：這次先用暫時結果，下次再重取
                    if (allReady == false)
                        return result;

                    _cachedMaterials = result;
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
