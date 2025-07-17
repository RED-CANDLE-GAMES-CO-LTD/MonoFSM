using System.Collections.Generic;
using Fusion.Addons.FSM;
using MonoFSM.Variable.Attributes;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    /// <summary>
    /// FIXME: 好像可以和 StateMachineLogic 合併成一個 MonoBehaviour
    /// </summary>
    //想要HFSM?
    public class MonoFSMOwner : MonoBehaviour, IStateMachineOwner
    {
        //只撈一層就是了
        [SerializeField] [CompRef] [AutoChildren(DepthOneOnly = true)]
        private MonoStateBehaviour[] _states; //SerializeField的話就可以略過不跑？

        [ShowInDebugMode] public StateMachine<MonoStateBehaviour> _fsm;

        void IStateMachineOwner.CollectStateMachines(List<IStateMachine> stateMachines)
        {
            var parent = GetComponentInParent<IStateMachineOwner>(true);
            if (parent == null)
            {
                Debug.LogError("MonoFSMOwner must be a child of StateMachineOwner.", this);
                return;
            }

            _fsm = new StateMachine<MonoStateBehaviour>(parent.name, _states);
            stateMachines.Add(_fsm);
        }

        public int GetCurrentStateId()
        {
            return _fsm.ActiveStateId;
        }

        //serialize state id? 用int就好？
        
    }
}