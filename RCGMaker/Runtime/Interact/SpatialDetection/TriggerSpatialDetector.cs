using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Runtime.Interact.EffectHit;
using RCGMaker.Runtime.Interact.SpatialDetection;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class TriggerSpatialDetector : SpatialDetector
    {
        [Auto] private Collider _collider;


        [AutoChildren] GeneralEffectDealer[] _effectDealers;
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("OnTriggerEnter");
            // ReliableOnTriggerExit.NotifyTriggerEnter(other, gameObject, OnTriggerExit);
            OnSpatialEnter(other.gameObject);
        }

        List<SpatialDetectable> toRemove = new List<SpatialDetectable>();
        private void OnDisable()
        {
            Debug.Log("OnDisable of detector",this);
            //copy _detectedObjects to toRemove
            toRemove.AddRange(_detectedObjects);
            foreach (var detectable in toRemove)
            {
                Debug.Log("OnDisable of detectable",detectable);
                OnTriggerExit(detectable.MyCollider);
            }
            toRemove.Clear();
        }

        private void OnTriggerExit(Collider other)
        {
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