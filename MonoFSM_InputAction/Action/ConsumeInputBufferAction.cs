using PlayerActionControl;

namespace RCGInputAction
{
    public class ConsumeInputBufferAction:AbstractStateAction
    {
        public PlayerBufferedInputAction listener;
        protected override void OnStateEnterImplement()
        {
            listener.ForceWasPressAction();
        }
    }
}