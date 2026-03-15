using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Runtime.Action;
using UnityEngine;

namespace MonoFSM_Physics.Runtime
{
    public class RigidbodyDirFollowVelocity : AbstractStateAction
    {
        //FIXME: 從Schema拿?
        // [SerializeField] private Rigidbody _rb;

        protected override void OnActionExecuteImplement()
        {
            var rb = BindEntity.GetSchema<ProjectileSchema>()._rigidbody;
            if (rb == null || rb.isKinematic)
                return;
            var v = rb.linearVelocity;
            if (v.sqrMagnitude > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(v, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 0.2f)); // 0.2f 可調
            }

            // Debug.Log("RigidbodyDirFollowVelocity set rot: " + v, this);
        }
    }
}
