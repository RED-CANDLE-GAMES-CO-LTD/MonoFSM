using MonoFSM_Core.Runtime.Action;

namespace RCGMaker.Runtime.ObjectPool
{
    //把FSM關掉，不一定是要回pool?
    public class StopFSMAction: AbstractStateAction
    {
        [AutoParent] private StateMachineOwner _owner;
        protected override void OnStateEnterImplement()
        {
            _owner.gameObject.SetActive(false); //FIXME: fusion要怎麼處理這個？
        }
    }
}