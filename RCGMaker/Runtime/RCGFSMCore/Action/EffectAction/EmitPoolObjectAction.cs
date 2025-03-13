namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.EffectAction
{
    //FIXME: 重做FXPlayer
    public class EmitPoolObjectAction : AbstractStateAction
    {
        public PoolObject poolObject;

        protected override void OnStateEnterImplement()
        {
            var newObj = PoolManager.Instance.BorrowOrInstantiate(poolObject, transform.position, transform.rotation);
        }

        public override void EventReceived(IEffectHitData arg)
        {
            // base.EventReceived(arg);
            //噴Receiver的位置?
            var t = arg.Receiver.transform;
            var newObj = PoolManager.Instance.BorrowOrInstantiate(poolObject, t.position, t.rotation);
        }
    }
}