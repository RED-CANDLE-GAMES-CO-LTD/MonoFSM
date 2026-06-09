using MonoFSM.Core;
using Sirenix.OdinInspector;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    public class OnCollisionHandler : AbstractEventHandler
    {
#if UNITY_EDITOR //看一下關聯
        [ShowInInspector] [AutoParent] private CollisionEventListener _collisionEventListener;
#endif
    }
}
