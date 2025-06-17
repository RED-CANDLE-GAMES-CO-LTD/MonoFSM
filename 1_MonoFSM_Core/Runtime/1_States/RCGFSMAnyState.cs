using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Core
{
    public class RCGFSMAnyState : MonoBehaviour, IState<GeneralState>, IDefaultSerializable
    {
        [AutoParent] private GeneralFSMContext context;

        public GeneralFSMContext Context => context;

        public bool TransitionCheck(GeneralState toState, float timeOffset = 0,
            global::StateTransition fromTransition = null)
        {
            // context.SetLastTransition(fromTransition);
            return TransitionCheck(toState);
        }

        public bool TransitionCheck(GeneralState toState)
        {
            // var fsm = context.fsm;
            // fsm.ChangeState(toState);

            return toState.ForceGoToState();
        }
        // public bool ForceTransition(GeneralState toState)
        // {
        //     var fsm = context.fsm;
        //     fsm.ChangeState(toState);
        //     return true;
        //     // return false;
        // }


        [AutoChildren] global::StateTransition[] transitions;

#if UNITY_EDITOR
        // [Button("Add Event Transition")]
        // public void AddEventTransitionEditor()
        // {
        //     AddEventTransition();
        // }
        //
        // public RCGEventReceiveTransition AddEventTransition()
        // {
        //     Undo.RecordObject(this, "Add To Transition List");
        //     var t = gameObject.AddChildrenComponent<RCGEventReceiveTransition>("[Transition] NewTransition");
        //     // Undo.RegisterCompleteObjectUndo()
        //     // Undo.IncrementCurrentGroup();
        //     transitions.Add(t);
        //     // EditorUtility.SetDirty(this);
        //     return t;
        // }
#endif
    }
}