using MonoFSM.Runtime.PhysicsAction;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    public class ParentRigidbodyProvider : MonoBehaviour, IRigidbodyProvider
    {
        [AutoParent] private Rigidbody _parentRigidbody;

        public Rigidbody GetRigidbody()
        {
            if (_parentRigidbody == null)
            {
                Debug.LogError("No Rigidbody found on parent of " + gameObject.name, this);
                return null;
            }

            return _parentRigidbody;
        }
    }
}