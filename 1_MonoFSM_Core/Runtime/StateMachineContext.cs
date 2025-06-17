using System;
using System.Collections;
using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Deprecated
{
    public abstract class StateMachineContext<T, TState> : MonoBehaviour, ISceneAwake, ISceneSavingCallbackReceiver
        where TState : AbstractState<T> where T : class 
    {
        [InfoBox("出現不能改但卻是Null，找易衡討論討論")]
        // public bool ShowStartState = true;
        [Required]
        // [DisallowModificationsIn(PrefabKind.Variant | PrefabKind.PrefabInstance)]
        [ValueDropdown(nameof(GetAllStates))]
        [DropDownRef]
        public TState startState;

        [PreviewInInspector] public T currentStateType => fsm?.State; //debug用
        public IEnumerable GetAllStates()
        {
            return GetComponentsInChildren<TState>();
        }

        // [HideFromSerialization]
        [NonSerialized]
        public StateMachine<T> fsm;

        // private void OnValidate()
        // {
        //     if(startState == null)
        //         Debug.LogError("為什麼沒有StartState?",gameObject);
        // }

        protected virtual void Awake()
        {

           
        }

        protected virtual void Start()
        {
            // var initType = startState.stateType;
            // fsm.ChangeState(initType);
        }

        //TODO:init?
        public void ChangeState(T stateType)
        {
            fsm.ChangeState(stateType,true);
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

        public void InitStateMachine()
        {
            if(fsm != null)
                return;
            var stateBehaviorMapping = new StateMapping<T>();

            // var stateDict = new Dictionary<T, TState>();
            foreach (var state in states)
            {
                // stateDict.Add(state.stateType, state);
                stateBehaviorMapping.AddStateBehaviorMapping(state.stateType, state, this);
            }

            // Debug.Log("StateMapping:" + stateBehaviorMapping.getAllStates.Count, this);
            fsm = StateMachine<T>.Initialize(this, stateBehaviorMapping);
            
        }

        // [AutoChildren] [PreviewInInspector] [SerializeField] TState[] serstates;
        [AutoChildren] [PreviewInInspector] protected TState[] states;
        // [AutoChildren] [PreviewInInspector] public TState[] pstates;
        public TState[] States => states;
       
        
        public void OnBeforeSceneSave()
        {
            var badMonos = gameObject.GetComponents<StateMachineRunner>();
            if (badMonos.Length > 1)
            {
                Debug.LogError("有多個StateMachineRunner", gameObject);
                foreach (var badMono in badMonos)
                {
                    DestroyImmediate(badMono);
                }
            }

            gameObject.TryGetCompOrAdd<StateMachineRunner>();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }

        public void EnterSceneAwake()
        {
            //FIXME: 場景call一次，poolObject又call一次...
            InitStateMachine();
        }
    }
}