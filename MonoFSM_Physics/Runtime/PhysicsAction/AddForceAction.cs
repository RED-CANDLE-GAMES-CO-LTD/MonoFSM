using MonoFSM.Core;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    //對一個rigidbody施加一個力 => 需要一個
    public class AddForceAction : AbstractStateAction
    {
        //型別指定會導致每一種型別都要寫一份捏，雖然數量有限？
        [CompRef] [AutoChildren] private ICompProvider<Rigidbody> _rigidbodyProvider;
        [CompRef] [AutoChildren] private IValueProvider<Vector3> _forceDirectionProvider;

        //FIXME: 不一定從hitdata來唷
        // [CompRef] [AutoParent] private IHitDataProvider _hitDataProvider;

        // [SerializeField] private Vector3 _torque;
        [FormerlySerializedAs("_torqueMagnitude")] [SerializeField]
        private float _magnitude = 10f; // 可以在Inspector中調整

        [SerializeField] private ForceMode _forceMode = ForceMode.Impulse;
        public ForcePosition _forcePosition = ForcePosition.TargetCenterOfMass; // 使用剛體的質心

        public enum ForcePosition
        {
            TargetCenterOfMass, // 使用剛體的質心
            ActionPosition // 使用Action所在的Transform位置
        }
//TODO: offset?
        private Rigidbody _cacheRigidbody;
        public void ArgEventReceived(Rigidbody target) //轉型Provider?
        {
            if (target == null)
            {
                Debug.LogError("No Rigidbody provided to AddForceAction", this);
                return;
            }

            _cacheRigidbody = target;
            // var hitData = _hitDataProvider.GetHitData();
            // var dir = hitData.Dealer.transform.position - hitData.Receiver.transform.position;
            // var _torque = ;
            // Debug.Log("AddForce: Applying torque to " + target.name + " with direction: " + dir, this);
            // Debug.DrawLine(hitData.Dealer.transform.position, hitData.Receiver.transform.position, Color.green, 10f);
            //Vector3 provider?
            //FIXME: hitdata的point?
            var dir = _forceDirectionProvider.Value * _magnitude;
            Debug.Log("AddForce: Applying torque to " + target.name + " with direction: " + dir, this);
            //怎麼用local space的方向？
            // var localDir = target.transform.TransformDirection(dir);
            var pos = _forcePosition == ForcePosition.ActionPosition
                ? transform.position // 使用Action所在的Transform位置
                : target.worldCenterOfMass; // 使用剛體的質心
            target.AddForceAtPosition(dir, pos,
                _forceMode); // 使用 AddForceAtPosition 來施加力
            
            // Debug.Break();
        }
        //
        // public void EventReceived<T>(T arg)
        // {
        //     // ArgEventReceived(arg as Rigidbody);
        // }

        //如果沒有額外的，用Receiver
        protected override void OnActionExecuteImplement()
        {
            // var hitData = _hitDataProvider.GetHitData();
            // if (hitData == null)
            // {
            //     Debug.LogError("HitData is null in AddTorqueAction", this);
            //     return;
            // }

            if (_rigidbodyProvider == null)
            {
                Debug.LogError("RigidbodyProvider is not set in AddforceAction", this);
                return;
            }

            var target = _rigidbodyProvider.Get();
            // var target = hitData.Receiver.transform.GetComponent<Rigidbody>();
            if (target == null)
            {
                Debug.LogError("No Rigidbody found on Receiver in AddTorqueAction", this);
                return;
            }

            ArgEventReceived(target);
        }
    }
}