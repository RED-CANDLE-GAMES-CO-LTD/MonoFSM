using System;
using RCGMaker.Runtime.Interact.EffectHit;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class TriggerSpatialDetector : SpatialDetector
    {
        [Auto] private Collider _collider;


        [AutoChildren] GeneralEffectDealer[] _effectDealers;
        private void OnTriggerEnter(Collider other)
        {
            OnSpatialEnter(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            OnSpatialExit(other.gameObject);
        }

        protected override void SetLayerOverride()
        {
            //FIXME:
            _collider.includeLayers = HittingLayer;
        }
    }
}