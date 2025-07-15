using MonoFSM.Core.Runtime.Action;
using PlayerActionControl;

namespace RCGInputAction
{
    public class ConsumeInputBufferAction : AbstractStateAction
    {
        public PlayerBufferedInputAction listener;

        protected override void OnActionExecuteImplement()
        {
            listener.ForceWasPressAction();
        }
    }
}