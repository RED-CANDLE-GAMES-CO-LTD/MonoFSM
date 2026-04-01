using UnityEngine;
using VInspector;

namespace Fusion.Addons.KCC._0_Gameplay.LineworkGlue
{
    public class RendererCollection : MonoBehaviour
    {
        public RendererCollection _rendererCollectionRef;

        [ShowInInspector] [AutoChildren] private Renderer[] _renderers;

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