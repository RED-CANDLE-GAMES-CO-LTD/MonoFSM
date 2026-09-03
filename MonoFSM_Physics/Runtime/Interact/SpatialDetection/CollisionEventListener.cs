using MonoFSM.Core;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection
{
    //FIXME: 要和EffectDetect整合？
    //必須裝載rigidbody上
    public class CollisionEventListener : MonoBehaviour, IParentEntityProvider
    {
#if UNITY_EDITOR
        [ShowInInspector] float _lastCollisionTime;
        [ShowInInspector] float _lastRelativeVelocity; //最近一次碰撞的相對速度，方便調門檻
        [ShowInInspector] float _lastImpulse; //最近一次碰撞的衝量(N·s)，含質量，方便調門檻
#endif
        [Header("碰撞事件過濾")]
        //衝量 = (1+e) * 有效質量 * 相對速度，單位 N·s。撞擊「多重」看這個，不是看速度
        [SerializeField] private float _minImpulse = 1f; //低於此衝量的碰撞不觸發
        [SerializeField] private float _minRelativeVelocity; //額外的速度門檻，0 = 不過濾
        [SerializeField] private float _cooldownDuration = 0.15f; //兩次觸發之間的最小間隔（秒）
        private float _lastTriggerTime = float.MinValue;

        //FIXME: photon 還沒準備好診麼辦？
        void OnCollisionEnter(Collision collision)
        {
#if UNITY_EDITOR
            _lastCollisionTime = Time.time;
            _lastRelativeVelocity = collision.relativeVelocity.magnitude;
            _lastImpulse = collision.impulse.magnitude;
#endif
            var impulse = collision.impulse.magnitude;
            if (impulse < _minImpulse)
            {
                Debug.Log($"[CollisionEventListener] impulse {impulse} < {_minImpulse}, skip", this);
                return;
            }

            if (_minRelativeVelocity > 0 && collision.relativeVelocity.magnitude < _minRelativeVelocity)
            {
                Debug.Log(
                    $"[CollisionEventListener] relativeVelocity {collision.relativeVelocity.magnitude} < {_minRelativeVelocity}, skip",
                    this);
                return;
            }
            if (Time.time - _lastTriggerTime < _cooldownDuration)
                return;
            _lastTriggerTime = Time.time;

            // Debug.Log("Collision Enter: " + collision.gameObject.name);
            if (_collisionImpluseMagnitude != null)
                _collisionImpluseMagnitude.SetValue(impulse);

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
                _hittingEntity?
                    .SetValue(
                        entity); //TODO: 這裡是碰撞到的物件，還是碰撞到的物件的 parent entity？要不要改成兩個變數分別存？（或 collision 直接丟出去讓 handler 自己決定要不要從裡面取？）
                //FIXME: gen effectHitData?
                _abstractEventHandler._hitEntity?.SetValue(entity);
            }

            //可能打到地板喔
            _abstractEventHandler._hitPosition?.SetValue(collision.contacts[0].point);
            _abstractEventHandler.EventHandle(collision); //float?
        }

        //FIXME: 從這裡接出來超怪，從CollisionHandler還可以？
        [SerializeField] VarEntity _hittingEntity;

        // public VarVector3 _collisionRelativeVelocity;
        [FormerlySerializedAs("_collisionVelocityMagnitude")]
        public VarFloat _collisionImpluseMagnitude;

        [DropDownRef]
        public OnCollisionHandler _abstractEventHandler;
        [AutoParent] private MonoEntity _parentEntity;
        public MonoEntity ParentEntity => _parentEntity;
    }
}
