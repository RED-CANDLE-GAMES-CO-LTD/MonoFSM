using System;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Detection
{
    public class TriggerSpatialDetector2D : AbstractDetector
    {
        [PreviewInInspector] [Auto] Collider2D _collider;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnDetectEnter(other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OnDetectExit(other.gameObject);
        }


        protected override void OnDisableImplement()
        {
        }

        protected override void SetLayerOverride()
        {
            _collider.includeLayers = HittingLayer;
        }
    }
}