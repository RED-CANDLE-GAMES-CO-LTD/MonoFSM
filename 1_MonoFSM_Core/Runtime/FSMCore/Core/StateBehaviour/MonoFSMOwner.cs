using System.Collections.Generic;
using Fusion.Addons.FSM;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    /// <summary>
    /// FIXME: 好像可以和 StateMachineLogic 合併成一個 MonoBehaviour
    /// </summary>
    [RequireComponent(typeof(StateMachineLogic))]
    public class MonoFSMOwner : MonoBehaviour, IStateMachineOwner
    {
        [SerializeField] [CompRef] [AutoChildren]
        private MonoStateBehaviour[] _states; //SerializeField的話就可以略過不跑？

        [ShowInDebugMode] private StateMachine<MonoStateBehaviour> _fsm;

        void IStateMachineOwner.CollectStateMachines(List<IStateMachine> stateMachines)
        {
            _fsm = new StateMachine<MonoStateBehaviour>(name, _states);
            stateMachines.Add(_fsm);
        }
    }
}