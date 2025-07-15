using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    public class SetRigidbodyKinematicAction : AbstractStateAction
    {
        [Required] [CompRef] [AutoChildren] private ICompProvider<Rigidbody> _rigidbodyProvider;
        public bool _isKinematic = true;

        protected override void OnActionExecuteImplement()
        {
            var rb = _rigidbodyProvider.Get();
            if (rb != null)
            {
                rb.isKinematic = _isKinematic;
                Debug.Log($"Set Rigidbody Kinematic: {rb.name} to {_isKinematic}", rb.gameObject);
            }
            else
            {
                Debug.LogError("Rigidbody not found in SetRigidbodyKinematicAction", this);
            }
        }
    }
}