using RCGMaker.Runtime.Interact.EffectHit;

namespace RCGMaker.Runtime.Interact.SpatialDetection
{
    public class MouseDownDetectable : EffectDetectable
    {
        public void HandleMouseDown(MouseDetector detector)
        {
            // if(detector.)
            // Debug.Log("OnMouseDown", this);
            detector.OnSpatialEnter(gameObject);
        }
    }
}