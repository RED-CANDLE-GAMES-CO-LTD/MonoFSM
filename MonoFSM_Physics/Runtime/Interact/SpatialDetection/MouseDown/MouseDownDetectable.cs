using MonoFSM.Runtime.Interact.EffectHit;

namespace MonoFSM.Runtime.Interact.SpatialDetection
{
    public class MouseDownDetectable : EffectDetectable
    {
        public void HandleMouseDown(MouseDetector detector)
        {
            // if(detector.)
            // Debug.Log("OnMouseDown", this);
            detector.OnDetectEnter(gameObject);
        }
    }
}