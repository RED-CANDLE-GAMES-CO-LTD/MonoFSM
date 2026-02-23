using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;

namespace MonoFSM.Core
{
    public class ResetTimerAction : AbstractStateAction
    {
        [DropDownRef] public VarFloatCountDownTimer timer;

        protected override void OnActionExecuteImplement()
        {
            timer.ResetTimer();
        }
    }
}
