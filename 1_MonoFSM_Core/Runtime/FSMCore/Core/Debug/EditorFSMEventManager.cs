using System;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion.Addons.FSM;

namespace MonoFSM.Editor
{
#if UNITY_EDITOR
    public static class EditorFsmEventManager //Editor bridge, 給Editor code接的
    {
        public static void NotifyStateChanged(StateMachineLogic logic)
        {
            OnStateChanged?.Invoke(logic);
        }

        public static event Action<StateMachineLogic> OnStateChanged;
    }
#endif
}