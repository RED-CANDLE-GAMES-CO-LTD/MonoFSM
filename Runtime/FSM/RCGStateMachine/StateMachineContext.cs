using UnityEngine;
using Sirenix.OdinInspector;

namespace RCGMaker.Core
{
    public abstract class StateMachineContext<T, TState> : MonoBehaviour
        where TState : AbstractState<T> //where T : ScriptableObject 
    {
        [InfoBox("企劃應該不用改這層！大家都去init state , Init Default Transition 決定初始狀態")]
        // public bool ShowStartState = true;
        [Required]
        [DisallowModificationsIn(PrefabKind.Variant)]
        public TState startState;
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
            var initType = startState.stateType;
            fsm.ChangeState(initType);
        }

        //TODO:init?
        public void ChangeState(T stateType)
        {
            fsm.ChangeState(stateType);
        }

        public TCustomState AddState<TCustomState>(System.Type type) where TCustomState : TState
        {
            var state = gameObject.AddChildrenComponent(type, "[State] NewState");
            return state as TCustomState;
        }

        public GeneralState AddState(System.Type type)
        {
            var state = gameObject.AddChildrenComponent(type, "[State] NewState");
            return state as GeneralState;
        }
    }
}