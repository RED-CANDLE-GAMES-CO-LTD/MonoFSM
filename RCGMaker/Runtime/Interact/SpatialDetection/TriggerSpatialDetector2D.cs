using System;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class TriggerSpatialDetector2D : SpatialDetector
    {
        [PreviewInInspector] [Auto] Collider2D _collider;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnSpatialEnter(other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OnSpatialExit(other.gameObject);
        }


        protected override void SetLayerOverride()
        {
            _collider.includeLayers = HittingLayer;
        }
    }
}