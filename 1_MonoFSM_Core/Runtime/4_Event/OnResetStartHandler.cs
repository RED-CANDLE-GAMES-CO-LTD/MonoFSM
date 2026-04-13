using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core
{
    public class OnResetStartHandler : AbstractEventHandler, IResetStart
    {
        public void ResetStart()
        {
            EventHandle();
        }
    }
}
