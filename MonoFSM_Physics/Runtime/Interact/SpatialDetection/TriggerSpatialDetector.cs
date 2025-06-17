using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace MonoFSM.Core.Detection
{
    public class TriggerSpatialDetector : AbstractDetector
    {
        public AbstractDetector virtualDetector;
        [Auto] private Collider _collider;

        private void OnTriggerEnter(Collider other)
        {
            this.Log("OnTriggerEnter", this);
            // ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);
            //FIXME: 先標記，再Update做
            virtualDetector?.OnSpatialEnter(other.gameObject);
            OnSpatialEnter(other.gameObject);
        }


        private void OnTriggerExit(Collider other)
        {
            //FIXME: 先標記，再Update做
            virtualDetector?.OnSpatialExit(other.gameObject);
            // ReliableOnTriggerExit.NotifyTriggerExit(other, gameObject);
            OnSpatialExit(other.gameObject);
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