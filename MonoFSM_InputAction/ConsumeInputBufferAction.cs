using PlayerActionControl;

namespace RCGInputAction
{
    public class ConsumeInputBufferAction:AbstractStateAction
    {
        public PlayerInputActionListener listener;
        protected override void OnStateEnterImplement()
        {
            listener.ForceWasPressAction();
        }
    }
}