using MonoFSM_Core.Runtime.Action;
using MonoFSM_Core.Simulate;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMaker.Core
{
    public class ResetTimerAction : AbstractStateAction
    {
        [DropDownRef] public VarFloatCountDownTimer timer;

        //指定到一個特定時間？
        [Component] [Auto] public IFloatProvider timeProvider;

        protected override void OnStateEnterImplement()
        {
            if (timeProvider != null)
                timer.SetTimer(timeProvider.Value);
            else
                timer.ResetTimer();
        }
    }
}