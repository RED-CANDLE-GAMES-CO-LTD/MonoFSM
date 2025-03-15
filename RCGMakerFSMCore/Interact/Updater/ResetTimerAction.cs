using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMaker.Core
{
    public class ResetTimerAction : AbstractStateAction
    {
        [DropDownRef] public VarFloatCountDownTimer timer;
        [Component] [Auto] public IFloatProvider timeProvider;

        protected override void OnStateEnterImplement()
        {
            if (timeProvider != null)
                timer.ResetTimer(timeProvider.Value);
            else
                timer.ResetTimer();
        }
    }
}