using System;
using UnityEngine;

namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.InstantiateAction
{
    //重寫FXPlayer
    public class InstantiateAction : AbstractStateAction
    {
        public PoolObject target;

        protected override void OnStateEnterImplement()
        {
            Debug.Log("InstantiateAction", this);
            PoolManager.Instance.BorrowOrInstantiate(target, transform.position, transform.rotation);
        }

        // private void OnEnable()
        // {
        //     OnStateEnterImplement();
        // }
        public override void EventReceived(IEffectHitData arg)
        {
            // base.EventReceived(arg);
            //噴Receiver的位置?
            var t = arg.Receiver.transform;
            var newObj = PoolManager.Instance.BorrowOrInstantiate(target, t.position, t.rotation);
        }
    }
}