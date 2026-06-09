using MonoFSM.Core;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    public class CollisionEventListener : MonoBehaviour
    {
#if UNITY_EDITOR
        [ShowInInspector] float _lastCollisionTime;
#endif
        //FIXME: photon 還沒準備好診麼辦？
        void OnCollisionEnter(Collision collision)
        {
#if UNITY_EDITOR
            _lastCollisionTime = Time.time;
#endif
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
