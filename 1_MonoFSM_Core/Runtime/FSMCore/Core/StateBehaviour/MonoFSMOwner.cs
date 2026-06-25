using System.Collections.Generic;
using Fusion.Addons.FSM;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    /// <summary>
    /// FIXME: 好像可以和 StateMachineLogic 合併成一個 MonoBehaviour
    /// </summary>
    //想要HFSM?
    public class MonoFSMOwner : MonoBehaviour, IStateMachineOwner
    {
        [CompRef]
        [Auto]
        private StateFolder _stateFolder;

        public StateFolder stateFolder => _stateFolder;

        // private List<MonoStateBehaviour> _states => _stateFolder.Collections;

        [ShowInDebugMode]
        public StateMachine<MonoStateBehaviour> _fsm;

        void IStateMachineOwner.CollectStateMachines(List<IStateMachine> stateMachines)
        {
            var owner = GetComponentInParent<IStateMachineOwner>(true);
            if (owner == null)
            {
                Debug.LogError("MonoFSMOwner must be a child of StateMachineOwner.", this);
                return;
            }

            if (owner.transform.parent == null)
            {
                Debug.LogError("MonoFSMOwner owner.transform.parent = null", this);
            }

            if (_stateFolder == null)
                _stateFolder = GetComponent<StateFolder>();

            if (_stateFolder == null)
            {
                Debug.LogError("MonoFSMOwner state folder not found", this);
            }

            //FIXME: 這個沒有nested喔
            //FIXME 會很早call, register networkObject時， word count
            _fsm = new StateMachine<MonoStateBehaviour>(owner.transform.parent.name,
                _stateFolder.AllValues);
            stateMachines.Add(_fsm);
        }

        public int GetCurrentStateId()
        {
            return _fsm.ActiveStateId;
        }

        public IState CurrentState => _fsm?.ActiveState;
        public IState PreviousState => _fsm?.PreviousState;
        public float DeltaTime { get; set; }

        public bool IsCurrentState(IState state)
        {
            if (state == null || _fsm == null)
                return false;
            return _fsm.ActiveState == state;
        }

        public int stateIdToRestore = -1;

        public void RestoreState(int stateId)
        {
            stateIdToRestore = stateId;
        }

        public void ForceActivateState(int stateId, bool allowReset = true)
        {
            _fsm?.ForceActivateState(stateId, allowReset);
        }

        //serialize state id? 用int就好？
    }
}
