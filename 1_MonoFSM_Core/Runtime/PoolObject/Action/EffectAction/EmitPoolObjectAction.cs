using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Interact.EffectHit;

namespace MonoFSM.Runtime.Variable.Action.EffectAction
{
    //FIXME: 重做FXPlayer
    //FIXME: 和InstantiateAction 重複了
    public class EmitPoolObjectAction : AbstractStateAction<GeneralEffectHitData>
    {
        public PoolObject poolObject;

        protected override void OnActionExecuteImplement()
        {
            var newObj = PoolManager.Instance.BorrowOrInstantiate(poolObject, transform.position, transform.rotation);
        }

        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            // base.EventReceived(arg);
            //噴Receiver的位置?
            var t = arg.Receiver.transform;
            var newObj = PoolManager.Instance.BorrowOrInstantiate(poolObject, t.position, t.rotation);
        }
    }
}