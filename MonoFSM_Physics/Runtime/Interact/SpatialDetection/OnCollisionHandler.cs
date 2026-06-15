using MonoFSM.Core;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{

    public class OnCollisionHandler : AbstractEventHandler
    {
        public VarEntity _hitEntity;
        public VarVector3 _hitPosition;
        
#if UNITY_EDITOR //看一下關聯
        [ShowInInspector] [AutoParent] private CollisionEventListener _collisionEventListener;
#endif
    }
}
