using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Attributes;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    public class SetRigidbodyKinematicAction : AbstractStateAction
    {
        [SerializeField] private Rigidbody _rigidbody;
        public bool _isKinematic = true;

        protected override void OnActionExecuteImplement()
        {
            // var rb = _rigidbodyProvider.Get();
            var rb = _rigidbody;
            if (rb != null)
            {
                rb.isKinematic = _isKinematic;
                // Debug.Log($"Set Rigidbody Kinematic: {rb.name} to {_isKinematic}", rb.gameObject);
            }
            else
            {
                Debug.LogError("Rigidbody not found in SetRigidbodyKinematicAction", this);
            }
        }
    }
}
