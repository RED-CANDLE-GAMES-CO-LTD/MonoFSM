using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using RCGMaker.Core;
// using RCG.StateMachine;


//FIXME: 不要在這綁了應該拿掉，用RCGArgEvent做掉
public class GeneralFSMContext : StateMachineContext<GeneralState, GeneralState>
{
#if UNITY_EDITOR

    // [Button("Open Graph")]
    // void OpenGraph()
    // {

    // }

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
    public GeneralState[] GetAllStates()
    {
        // if (states == null)
        states = GetComponentsInChildren<GeneralState>();
        return states;
    }

    [ShowInInspector, ReadOnly]
    private GeneralState[] states;
    [ReadOnly]
    public AbstractStateTransition lastTransition;
    [ReadOnly]
    // public RCGEventBinding[] eventBindings; //TODO:這樣有比較好看懂嗎...？
    protected override void Awake()
    {
        base.Awake();
        //TODO: getComponents?
        //GenerateBindingTable
    }

    [AutoParent(false)] 
    public StateMachineOwner fsmOwner;

#if UNITY_EDITOR


    // private void GetBindingTable()
    // {
    //     var owner = GetComponentInParent<StateMachineOwner>();
    //     if (owner == null)
    //     {
    //         return;
    //     }
    //     var senders = owner.GetComponentsInChildren<RCGEventWrapper>(true);
    //     var receivers = GetComponentsInChildren<RCGEventReceiveTransition>(true);
    //     var dict = new Dictionary<RCGEventType, RCGEventBinding>();
    //     // var binding = new EventBinding();
    //     foreach (var sender in senders)
    //     {
    //         var type = sender.type;
    //         if (!dict.ContainsKey(type))
    //         {
    //             dict.Add(type, new RCGEventBinding(type));
    //         }
    //         dict[type].eventSenders.Add(sender);
    //     }
    //
    //     // foreach (var receiver in receivers)
    //     // {
    //     //     var type = receiver.eventType;
    //     //     if (type == null)
    //     //     {
    //     //         // Debug.LogError("receiver event not assign" + receiver.eventType, receiver);
    //     //         continue;
    //     //     }
    //     //     if (!dict.ContainsKey(type))
    //     //     {
    //     //         dict.Add(type, new RCGEventBinding(type));
    //     //     }
    //     //     dict[type].eventReceivers.Add(receiver);
    //     // }
    //     
    //     
    //     eventBindings = new RCGEventBinding[dict.Values.Count];
    //     dict.Values.CopyTo(eventBindings, 0);
    //     // 
    // }
#endif

}
