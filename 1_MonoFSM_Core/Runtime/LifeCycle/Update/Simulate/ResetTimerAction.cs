using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using UnityEngine;

namespace MonoFSM.Core
{
    public class ResetTimerAction : AbstractStateAction
    {
        [DropDownRef] public VarFloatCountDownTimer timer;

        //指定到一個特定時間？
        [Component] [Auto] public IFloatProvider timeProvider;

        protected override void OnActionExecuteImplement()
        {
            if (timeProvider != null)
                timer.SetTimer(timeProvider.Value);
            else
                timer.ResetTimer();
        }
    }
}