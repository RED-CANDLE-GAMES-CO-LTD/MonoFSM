using System;
using MonoFSM.Core.Runtime.Interact.SpatialDetection;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Detection
{
    public class CollisionSpatialDetector : AbstractDetector
    {
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private CollisionEventNode _enterNode;
        private void OnCollisionEnter(Collision other)
        {
            _enterNode?.EventHandle(other);
            OnDetectEnter(other.gameObject);
        }

        private void OnCollisionExit(Collision other)
        {
            OnDetectExit(other.gameObject);
        }

        //FIXME:
        protected override void OnDisableImplement()
        {
        }

        protected override void SetLayerOverride()
        {
            throw new System.NotImplementedException();
        }
    }
}