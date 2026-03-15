using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// FIXME: 會被撈出來當作 state 0, 白痴耶！
    /// </summary>
    public class AnyState : MonoStateBehaviour, IState<GeneralState>, IDefaultSerializable,
        IBeforePrefabSaveCallbackReceiver
    {
        //寫得好混亂///
        // [CompRef] [AutoChildren] public TransitionBehaviour<MonoStateBehaviour>[] _transitions;
        public bool TransitionCheck(GeneralState toState)
        {
            // var fsm = context.fsm;
            // fsm.ChangeState(toState);

            return toState.TryActivateState();
        }


        public void OnBeforePrefabSave()
        {
            if (transform.GetSiblingIndex() == 0)
            {
                Debug.LogError(
                    $"[AnyState] State '{Name}' is at index 0 in the StateFolder, which is reserved for the default state. Please move it to a non-zero index.",
                    this);
            }
            // if (StateId == 0)
            // {
            //     Debug.LogError(
            //         $"[AnyState] State '{Name}' has StateId 0, which is reserved for the default state. Please change it to a non-zero value.",
            //         this);
            // }
        }
    }
}
