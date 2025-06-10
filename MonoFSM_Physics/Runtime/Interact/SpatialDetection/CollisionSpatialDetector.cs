using System;
using MonoFSM_Core.Runtime.Interact.SpatialDetection;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace RCGMaker.Core.Detection
{
    public class CollisionSpatialDetector : AbstractDetector
    {
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private CollisionEventNode _enterNode;
        private void OnCollisionEnter(Collision other)
        {
            _enterNode?.EventHandle(other);
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