using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.PhysicsAction
{
    //GetComponentInParent?

    //FIXME: 不該用這個？應該用findVariableFromOwner?


    //從EffectHitData嗎？ 對象是Rigidbody, 方向
    public class AddTorqueAction : AbstractArgEventHandler<GeneralEffectHitData>
    {
        // [CompRef] [AutoParent] private ICompProvider<Rigidbody> _rigidbodyProvider;
        // [CompRef] [AutoParent] private IHitDataProvider _hitDataProvider;
        public Rigidbody _rb;
        public VarComp _rigidbodyVar;
        private Rigidbody rb => _rb ? _rb : _rigidbodyVar.Value as Rigidbody;

        public VarVector3Wrapper _torqueVector;

        // [SerializeField] private Vector3 _torque;
        [SerializeField]
        private float _torqueMagnitude = 10f; // 可以在Inspector中調整

        [SerializeField]
        private ForceMode _forceMode = ForceMode.Impulse;


        [Button]
        void TestAction()
        {
            OnActionExecuteImplement();
        }

        protected override void OnActionExecuteImplement()
        {
            // Delay();
            var bd = rb;
            var torqueDir = _torqueVector.Value * _torqueMagnitude;
            // bd.AddTorque(torqueDir, _forceMode);
            Vector3 spinAxis = Vector3.up;

            // 施加旋轉力矩 (AddTorque)
            // 這會讓物件像陀螺一樣沿著 Y 軸原地瘋狂自轉
            bd.AddTorque(spinAxis * _torqueMagnitude, _forceMode);
            Debug.Log(
                "AddTorqueAction: spinAxis "
                + spinAxis
                + " with direction: "
                + _torqueMagnitude,
                this
            );
        }

        private async void Delay()
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(0.1f);

                // Debug.Log(
                //     "AddTorqueAction: Applying force to "
                //         + bd.name
                //         + " with direction: "
                //         + torqueDir,
                //     this
                // );
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            //好像拿不到耶？
            var hitData = arg;
            var dir = hitData.Dealer.transform.position - hitData.Receiver.transform.position;

            // var _torque = ;
            // Debug.Log("AddTorqueAction: Applying torque to " + target.name + " with direction: " + dir, this);
            Debug.DrawLine(
                hitData.Dealer.transform.position,
                hitData.Receiver.transform.position,
                Color.red,
                10f
            );
            var bd = rb;
            bd.AddTorque(dir.normalized * _torqueMagnitude, _forceMode);
        }
    }
}
