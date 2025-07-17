using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Interact.SpatialDetection
{
    public class CollisionEventNode : AbstractEffectNode, ICollisionDataProvider
    {
        public void EventHandle(Collision collision)
        {
            _cacheCollision = collision;
            foreach (var receiver in _eventReceivers)
                if (receiver.isActiveAndEnabled)
                {
                    if (receiver is IArgEventReceiver<Collision> argReceiver)
                        argReceiver.ArgEventReceived(collision);
                    else
                        receiver.EventReceived();
                }
        }

        public Collision GetCollision()
        {
            return _cacheCollision;
        }

        [PreviewInInspector] private Collision _cacheCollision;
    }
}