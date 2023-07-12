using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace RCGMaker.Core
{
    public abstract class StateMachineContext<T, TState> : MonoBehaviour
        where TState : AbstractState<T> //where T : ScriptableObject 
    {
        [InfoBox("出現不能改但卻是Null，找易衡討論討論")]
        // public bool ShowStartState = true;
        [Required]
        // [DisallowModificationsIn(PrefabKind.Variant | PrefabKind.PrefabInstance)]
        [ValueDropdown("GetAllStates")]
        [DropDownRef]
        public TState startState;

        public T currentStateType; //debug用
        public IEnumerable GetAllStates()
        {
            return GetComponentsInChildren<TState>();
        }
        
        public StateMachine<T> fsm;

        private void OnValidate()
        {
            if(startState == null)
                Debug.LogError("為什麼沒有StartState?",gameObject);
        }

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
            // var initType = startState.stateType;
            // fsm.ChangeState(initType);
        }

        //TODO:init?
        public void ChangeState(T stateType)
        {
            fsm.ChangeState(stateType);
            currentStateType = stateType; //debug用
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