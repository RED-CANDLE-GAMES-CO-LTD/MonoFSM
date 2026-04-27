using MonoFSM.Core;
using MonoFSM.Variable;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    public class CollisionEventListener : MonoBehaviour
    {
        void OnCollisionEnter(Collision collision)
        {
            // Debug.Log("Collision Enter: " + collision.gameObject.name);
            if (_collisionImpluseMagnitude != null)
                _collisionImpluseMagnitude.SetValue(collision.impulse.magnitude);
            _abstractEventHandler.EventHandle(collision); //float?

        }

        // public VarVector3 _collisionRelativeVelocity;
        [FormerlySerializedAs("_collisionVelocityMagnitude")]
        public VarFloat _collisionImpluseMagnitude;

        public OnCollisionHandler _abstractEventHandler;
    }
}
