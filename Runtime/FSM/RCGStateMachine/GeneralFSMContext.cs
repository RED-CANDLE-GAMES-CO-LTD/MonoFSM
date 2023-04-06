using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

// using RCG.StateMachine;

namespace RCGMaker.Core
{
    //TODO: state folder?
    //FIXME: 不要在這綁了應該拿掉，用RCGArgEvent做掉
    public class GeneralFSMContext : StateMachineContext<GeneralState, GeneralState>
    {
#if UNITY_EDITOR

        // [Button("Open Graph")]
        // void OpenGraph()
        // {

        // }

        public T AddState<T>() where T : GeneralState
        {
            var state = gameObject.AddChildrenComponent<T>("[State] NewState");
            return state;
        }

       


        public GeneralState AddState()
        {
            var state = gameObject.AddChildrenComponent<GeneralState>("[State] NewState");
            return state;
        }

        [Button("Add State")]
        void AddStateVoid()
        {
            AddState();
        }

        [Button("Open Graph")]
        void OpenGraph()
        {
            Selection.activeGameObject = gameObject;
            EditorApplication.ExecuteMenuItem("Window/FSMGraphView Window");
            // EditorWindow.GetWindow(System.Type.GetType("FSMGraphEditorWindow"));
        }
#endif
        public List<GeneralState> GetAllStates()
        {
            if (states == null)
                states = new List<GeneralState>();
            states.Clear();
            GetComponentsInChildren<GeneralState>(states);
            return states;
        }

        List<GeneralState> states;

        [ReadOnly] public AbstractStateTransition lastTransition;

        // [ReadOnly]
        // public RCGEventBinding[] eventBindings; //TODO:這樣有比較好看懂嗎...？
        protected override void Awake()
        {
            base.Awake();
            //TODO: getComponents?
            //GenerateBindingTable
        }

        //把Event分在一起，Transition分會比較好嗎??
#if UNITY_EDITOR
        private void OnValidate()
        {
            GetBindingTable();
        }

        private void Reset()
        {
            GetBindingTable();
        }

        private void GetBindingTable()
        {
            var owner = GetComponentInParent<StateMachineOwner>();
            if (owner == null)
            {
                return;
            }
        }
#endif

        // public void ChangeState(GeneralStateType newState)
        // {
        //     fsm.ChangeState(newState, true);
        // }
    }
}