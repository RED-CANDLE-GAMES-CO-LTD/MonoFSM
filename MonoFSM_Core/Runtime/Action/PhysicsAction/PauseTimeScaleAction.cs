using MonoFSM_Core.Runtime.Action;

namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.PhysicsAction
{
    public class PauseTimeScaleAction : AbstractStateAction
    {
        protected override void OnStateEnterImplement()
        {
            RCGTime.Pause();
        }
    }
}