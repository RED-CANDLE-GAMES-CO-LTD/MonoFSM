using MonoFSM.Core;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{


    public class OnCollisionHandler : AbstractEventHandler
    {
        public VarEntity _hitEntity; //這要幹啥？
        public VarVector3 _hitPosition;

#if UNITY_EDITOR //看一下關聯
        //FIXME: 確保有放在rigidbody上？
        [Required]
        [ShowInInspector]
        private CollisionEventListener collisionEventListener =>
            _parentEntity?.GetComponentInChildren<CollisionEventListener>();

        [ShowInInspector] [AutoParent] MonoEntity _parentEntity;
#endif
    }
}
