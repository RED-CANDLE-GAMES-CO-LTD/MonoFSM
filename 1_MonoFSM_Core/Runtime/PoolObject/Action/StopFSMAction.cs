using MonoFSM.Core.Runtime.Action;

namespace MonoFSM.Runtime.ObjectPool
{
    //把FSM關掉，不一定是要回pool?
    public class StopFSMAction: AbstractStateAction
    {
        [AutoParent] private MonoEntity _owner;

        protected override void OnActionExecuteImplement()
        {
            _owner.gameObject.SetActive(false); //FIXME: fusion要怎麼處理這個？
        }
    }
}