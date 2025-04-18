using MonoFSM.Core;

namespace RCGMaker.Core.Module
{
    public class OnDisableNode : AbstractEventHandler
    {
        protected override bool ShouldHandleEvent(IEventReceiver eventReceiver)
        {
            return true;
        }
    }
}