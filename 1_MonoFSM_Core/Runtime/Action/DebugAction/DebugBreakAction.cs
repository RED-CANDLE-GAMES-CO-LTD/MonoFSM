using MonoFSM.Core.Runtime.Action;

namespace RCGMakerFSMCore.Runtime.Action.DebugAction
{
    public class DebugBreakAction:AbstractStateAction
    {
        protected override void OnStateEnterImplement()
        {
            this.Break();
        }
    }
}