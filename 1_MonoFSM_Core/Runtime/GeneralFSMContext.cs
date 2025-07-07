using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using System;

// using RCG.StateMachine;
[Obsolete]
public class GeneralFSMContext : MonoBehaviour
{
}

//FIXME: 不要在這綁了應該拿掉，用RCGArgEvent做掉
// [RuntimeQA(typeof(StateMachineQATestCase))]
// [Searchable]
// public class GeneralFSMContext : StateMachineContext<GeneralState, GeneralState>, //IQATarget<StateMachineQATestCase>,
//     IDefaultSerializable
// {
// #if UNITY_EDITOR
//
//     public void Pause()
//     {
//         fsm.Pause();
//     }
//
//     private void OnDrawGizmosSelected() //夠靠近？debugSensor範圍內?
//     {
//         if (!Application.isPlaying)
//             return;
//         //draw label of fsm.State.name
//         var oriState = randomState;
//         Random.state = randomState;
//         if (fsm != null && fsm.State != null)
//         {
//             var state = fsm.State;
//             var label = state.name;
//             var pos = transform.position;
//
//
//             //FIXME: 兩個東西可能會重疊...隨機往四個方向？
//             var offset = new Vector3(Random.Range(0, 10) + 10, Random.Range(0, -10) - 10, 0);
//             //set font size to 24
//             Handles.Label(pos + offset, label, new GUIStyle()
//             {
//                 fontSize = 16,
//                 normal = new GUIStyleState()
//                 {
//                     textColor = Color.white
//                 }
//             });
//
//             //draw a marker
//             Gizmos.color = Color.red;
//             Gizmos.DrawSphere(pos, 0.5f);
//         }
//
//         Random.state = oriState;
//     }
//
//
//     [ShowInPlayMode] public bool IsPaused => fsm != null ? fsm.isPaused : false;
//
//
//     public GeneralState AddState()
//     {
//         var state = gameObject.AddChildrenComponent<GeneralState>("[State] NewState");
//         return state;
//     }
//
//     [Button("Add State")]
//     void AddStateVoid()
//     {
//         AddState();
//     }
//
//     [Button("Open Graph")]
//     void OpenGraph()
//     {
//         Selection.activeGameObject = gameObject;
//         EditorApplication.ExecuteMenuItem("Window/FSMGraphView Window");
//         // EditorWindow.GetWindow(System.Type.GetType("FSMGraphEditorWindow"));
//     }
// #endif
//
//
//     //FIXME: 被culling掉的東西一打開要functional 空降狀態
//     public void SimulationUpdate()
//     {
//         // StateMachineTimeStamp += deltaTime;
//         // var passedDuration = Time.time - fsm.LastActiveTime;
//         // while (passedDuration > 0)
//         // {
//         //     var currentStateDurationLeft = fsm.State.StateDuration - fsm.State.statusTimer;
//         //
//         //     if (passedDuration > currentStateDurationLeft)
//         //     {
//         //         if (fsm.State.NextState == null)
//         //         {
//         //             //沒有下一個state，結束
//         //             break;
//         //         }
//         //
//         //         passedDuration -= currentStateDurationLeft; //扣掉這個state所還要花的時間
//         //         fsm.ChangeState(fsm.State.NextState);
//         //     }
//         //     else
//         //     {
//         //         fsm.State.SimulationUpdate(passedDuration);
//         //         break;
//         //     }
//         // }
//     }
//
//     public void PauseFSM() //被culling時
//     {
//         fsm.Pause();
//
//         foreach (var state in states)
//         {
//             state.Pause();
//         }
//     }
//
//     public void ResumeFSM()
//     {
//         fsm.Resume();
//         foreach (var state in states)
//         {
//             state.Resume();
//         }
//     }
//
//     private Random.State randomState;
//
//     public GeneralState[] GetAllGeneralStates()
//     {
//         // if (states == null)
//         states = GetComponentsInChildren<GeneralState>();
//         return states;
//     }
//
//
//     private StateTransition _lastTransition;
//
//     [PreviewInInspector] public StateTransition LastTransition => _lastTransition;
//
//     public void SetLastTransition(StateTransition transition)
//     {
//         _lastTransition = transition;
//     }
//
//     [PreviewInInspector] public GeneralState LastState => fsm?.LastState;
//
//     // [ReadOnly]
//     // public RCGEventBinding[] eventBindings; //TODO:這樣有比較好看懂嗎...？
//     protected override void Awake()
//     {
//         base.Awake();
//         randomState = Random.state;
//         //TODO: getComponents?
//         //GenerateBindingTable
//     }
//
//     [AutoParent(false)] public StateMachineOwner fsmOwner;
//
// #if UNITY_EDITOR
//
//
//     // private void GetBindingTable()
//     // {
//     //     var owner = GetComponentInParent<StateMachineOwner>();
//     //     if (owner == null)
//     //     {
//     //         return;
//     //     }
//     //     var senders = owner.GetComponentsInChildren<RCGEventWrapper>(true);
//     //     var receivers = GetComponentsInChildren<RCGEventReceiveTransition>(true);
//     //     var dict = new Dictionary<RCGEventType, RCGEventBinding>();
//     //     // var binding = new EventBinding();
//     //     foreach (var sender in senders)
//     //     {
//     //         var type = sender.type;
//     //         if (!dict.ContainsKey(type))
//     //         {
//     //             dict.Add(type, new RCGEventBinding(type));
//     //         }
//     //         dict[type].eventSenders.Add(sender);
//     //     }
//     //
//     //     // foreach (var receiver in receivers)
//     //     // {
//     //     //     var type = receiver.eventType;
//     //     //     if (type == null)
//     //     //     {
//     //     //         // Debug.LogError("receiver event not assign" + receiver.eventType, receiver);
//     //     //         continue;
//     //     //     }
//     //     //     if (!dict.ContainsKey(type))
//     //     //     {
//     //     //         dict.Add(type, new RCGEventBinding(type));
//     //     //     }
//     //     //     dict[type].eventReceivers.Add(receiver);
//     //     // }
//     //     
//     //     
//     //     eventBindings = new RCGEventBinding[dict.Values.Count];
//     //     dict.Values.CopyTo(eventBindings, 0);
//     //     // 
//     // }
// #endif
//
//
//     public bool IsQATestNeeded => true;
// }