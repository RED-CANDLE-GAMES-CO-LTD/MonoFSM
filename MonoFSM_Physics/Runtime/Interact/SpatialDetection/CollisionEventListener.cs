using MonoFSM.Core;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    //FIXME: 要和EffectDetect整合？
    public class CollisionEventListener : MonoBehaviour, IParentEntityProvider
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

            var rb = collision.collider.attachedRigidbody;
            if (rb != null)
            {
                var entityProvider = rb.GetComponent<IParentEntityProvider>();
                if (entityProvider == null)
                {
                    Debug.LogError(
                        $"[CollisionEventListener] The Rigidbody {rb.name} does not have a component that implements IParentEntityProvider. Collision event will not be handled properly.",
                        rb);
                }

                var entity = entityProvider?.ParentEntity;
                if (entity == null)
                {
                    Debug.LogError(
                        $"[CollisionEventListener] The Rigidbody {rb.name} does not have a parent entity. Collision event will not be handled properly.",
                        rb);
                }

                //從這裡接好醜？
                _hittingEntity
                    .SetValue(
                        entity); //TODO: 這裡是碰撞到的物件，還是碰撞到的物件的 parent entity？要不要改成兩個變數分別存？（或 collision 直接丟出去讓 handler 自己決定要不要從裡面取？）
                //FIXME: gen effectHitData?
                _abstractEventHandler._hitEntity?.SetValue(entity);
            }

            //可能打到地板喔
            _abstractEventHandler._hitPosition?.SetValue(collision.contacts[0].point);
            _abstractEventHandler.EventHandle(collision); //float?

        }

        [SerializeField] VarEntity _hittingEntity;

        // public VarVector3 _collisionRelativeVelocity;
        [FormerlySerializedAs("_collisionVelocityMagnitude")]
        public VarFloat _collisionImpluseMagnitude;

        public OnCollisionHandler _abstractEventHandler;
        [AutoParent] private MonoEntity _parentEntity;
        public MonoEntity ParentEntity => _parentEntity;
    }
}
