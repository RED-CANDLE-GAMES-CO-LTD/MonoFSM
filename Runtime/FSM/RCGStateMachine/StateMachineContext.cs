using UnityEngine;
using Sirenix.OdinInspector;

namespace RCGMaker.Core
{


    public abstract class StateMachineContext<T, TState> : MonoBehaviour
        where TState : AbstractState<T> //where T : ScriptableObject 
    {
        [InfoBox("企劃應該不用改這層！大家都去init state , Init Default Transition 決定初始狀態")]
        public bool ShowStartState = false;

        [ShowIf("ShowStartState")] public TState startState;
        public StateMachine<T> fsm;

        protected virtual void Awake()
        {
            StateMapping<T> stateBehaviorMapping = new StateMapping<T>();
            var states = GetComponentsInChildren<TState>();
            // var stateDict = new Dictionary<T, TState>();
            foreach (var state in states)
            {
                // stateDict.Add(state.stateType, state);
                stateBehaviorMapping.AddStateBehaviorMapping(state.stateType, state, this);
            }

            fsm = StateMachine<T>.Initialize(this, stateBehaviorMapping);

        }

        protected virtual void Start()
        {
            fsm.ChangeState(startState.stateType);
        }

        //TODO:init?
        public void ChangeState(T stateType)
        {
            fsm.ChangeState(stateType);
        }
    }
}