using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.Interact.EffectHit;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    public class CollisionEventNode : AbstractEffectNode, ICollisionDataProvider
    {
        public void EventHandle(Collision collision)
        {
//FIXME: 
//哪種action?
            Debug.Log("CollisionEventNode EventHandle", this);
            _cacheCollision = collision;
            foreach (var receiver in _eventReceivers)
                if (receiver.isActiveAndEnabled)
                    receiver.EventReceived(collision);
        }

        public Collision GetCollision()
        {
            return _cacheCollision;
        }

        [PreviewInInspector] private Collision _cacheCollision;
    }
}