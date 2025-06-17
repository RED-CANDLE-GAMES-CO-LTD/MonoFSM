using MonoFSM.Core.Runtime.Action;

namespace MonoFSM.Runtime.FSM.RCGStateMachine.Action.PhysicsAction
{
    public class PauseTimeScaleAction : AbstractStateAction
    {
        protected override void OnStateEnterImplement()
        {
            RCGTime.Pause();
        }
    }
}