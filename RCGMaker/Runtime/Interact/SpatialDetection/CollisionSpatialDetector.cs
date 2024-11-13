using System;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class CollisionSpatialDetector : SpatialDetector
    {
        private void OnCollisionEnter(Collision other)
        {
            Debug.Log("OnCollisionEnter");
            OnSpatialEnter(other.gameObject);
        }

        private void OnCollisionExit(Collision other)
        {
            OnSpatialExit(other.gameObject);
        }

        //FIXME:
        protected override void SetLayerOverride()
        {
            throw new System.NotImplementedException();
        }
    }
}