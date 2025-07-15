using MonoFSM.Core.Runtime.Action;

namespace MonoFSM.Runtime.Variable.Action.PhysicsAction
{
    public class PauseTimeScaleAction : AbstractStateAction
    {
        protected override void OnActionExecuteImplement()
        {
            RCGTime.Pause();
        }
    }
}