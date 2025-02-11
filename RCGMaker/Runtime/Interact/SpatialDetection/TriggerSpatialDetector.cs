using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Interact.EffectHit;
using RCGMaker.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class TriggerSpatialDetector : AbstractDetector
    {
        public AbstractDetector virtualDetector;
        [Auto] private Collider _collider;

        private void OnTriggerEnter(Collider other)
        {
            this.Log("OnTriggerEnter",this);
            // ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);
            virtualDetector?.OnSpatialEnter(other.gameObject);
            OnSpatialEnter(other.gameObject);
        }

       

        private void OnTriggerExit(Collider other)
        {
            virtualDetector?.OnSpatialExit(other.gameObject);
            // ReliableOnTriggerExit.NotifyTriggerExit(other, gameObject);
            OnSpatialExit(other.gameObject);
        }

        protected override void SetLayerOverride()
        {
            //FIXME:
            // _collider.includeLayers = HittingLayer;
        }
    }
}