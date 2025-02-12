namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.EffectAction
{
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
            var newObj = PoolManager.Instance.BorrowOrInstantiate(poolObject, transform.position, transform.rotation);
        }
    }
}