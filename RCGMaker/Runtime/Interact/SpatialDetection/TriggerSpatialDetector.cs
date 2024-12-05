using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Interact.EffectHit;
using RCGMaker.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class TriggerSpatialDetector : SpatialDetector
    {
        public SpatialDetector virtualDetector;
        [Auto] private Collider _collider;
        
        private void OnTriggerEnter(Collider other)
        {
            this.Log("OnTriggerEnter",this);
            // ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);
            virtualDetector?.OnSpatialEnter(other.gameObject);
            OnSpatialEnter(other.gameObject);
        }

        List<SpatialDetectable> toRemove = new List<SpatialDetectable>();
        //FIXME: Receiver的部分要怎麼處理？ 也會有開關的問題？還是沒差遇到再說
        private void OnDisable()
        {
            // Debug.Log("OnDisable of detector",this);
            //copy _detectedObjects to toRemove
            toRemove.AddRange(_detectedObjects);
            foreach (var detectable in toRemove)
            {
                // Debug.Log("OnDisable of detectable",detectable);
                OnTriggerExit(detectable.MyCollider);
            }
            toRemove.Clear();
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