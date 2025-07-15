using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    public class LerpToPositionAction : AbstractStateAction
    {
        [Required] [CompRef] [AutoChildren] private ICompProvider<Rigidbody> _rigidbodyProvider;
        public Vector3 _offsetPosition = Vector3.zero;

        protected override void OnActionExecuteImplement()
        {
            var rb = _rigidbodyProvider.Get();
            rb.isKinematic = true;
            var targetPosition = rb.transform.position + _offsetPosition;
            // 使用Lerp方法平滑移动到目标位置
            rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, bindingState.DeltaTime));
        }
    }
}