using UnityEngine;

namespace MonoFSM.Core.Detection
{
    public class TriggerDetector : AbstractDetector
    {
        public AbstractDetector virtualDetector;
        [Auto] private Collider _collider;

        private void OnTriggerEnter(Collider other)
        {
            this.Log("OnTriggerEnter", other);
            // ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);
            //FIXME: 先標記，再Update做
            virtualDetector?.OnDetectEnter(other.gameObject);
            OnDetectEnter(other.gameObject);
        }


        private void OnTriggerExit(Collider other)
        {
            //FIXME: 先標記，再Update做
            virtualDetector?.OnDetectExit(other.gameObject);
            // ReliableOnTriggerExit.NotifyTriggerExit(other, gameObject);
            OnDetectExit(other.gameObject);
        }

        protected override void OnDisableImplement()
        {
        }

        protected override void SetLayerOverride()
        {
            //FIXME:
            // _collider.includeLayers = HittingLayer;
        }
    }
}