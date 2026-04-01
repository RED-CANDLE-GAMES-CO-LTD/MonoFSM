using Fusion.Addons.KCC;
using Fusion.Addons.KCC._0_Gameplay.LineworkGlue;
using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;

namespace Gameplay
{
    public class SetRenderingLayerMaskAction : AbstractStateAction
    {
        [DropDownRef] public RendererCollection _rendererCollection;

        [EnumToggleButtons] public RenderingLayer _renderingLayer = RenderingLayer.Default;

        protected override void OnActionExecuteImplement()
        {
            if (_rendererCollection == null)
                return;

            _rendererCollection.SetRenderingLayerMask((uint)_renderingLayer);
        }

        public override string Description =>
            $"Set RenderingLayerMask to {_renderingLayer}";
    }

    [System.Flags]
    public enum RenderingLayer
    {
        Nothing = 0,
        Default = 1 << 0,
        LightLayer1 = 1 << 1,
        LightLayer2 = 1 << 2,
        LightLayer3 = 1 << 3,
        LightLayer4 = 1 << 4,
        LightLayer5 = 1 << 5,
        LightLayer6 = 1 << 6,
        LightLayer7 = 1 << 7,
    }
}