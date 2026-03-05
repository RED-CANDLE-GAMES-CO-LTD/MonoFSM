using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;

namespace MonoFSM.Core
{
    // [Obsolete("Obsolete")]
    public class AnyState : MonoStateBehaviour, IState<GeneralState>, IDefaultSerializable
    {

        public bool TransitionCheck(GeneralState toState)
        {
            // var fsm = context.fsm;
            // fsm.ChangeState(toState);

            return toState.TryActivateState();
        }
    }
}
