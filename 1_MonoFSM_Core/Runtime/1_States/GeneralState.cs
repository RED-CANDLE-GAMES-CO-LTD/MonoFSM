using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Threading;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion.Addons.FSM;

//FIXME: 可以拿掉？
public interface IGuidEntity
{
}

public interface ISerializableComponent
{
    public string Serialize();
    public void Deserialize(string data);
}

public interface IDefaultSerializable { }

public interface IReferenceTarget //FIXME: 這樣只有我自己寫的型別可以用？
{
}


[Searchable]
public class GeneralState : MonoStateBehaviour
{
    public float statusTimer => Machine.StateTime;

    public bool ForceGoToState() //FIXME: 拿掉這個
    {
        return Machine.TryActivateState(this);
    }

    [Button("強制跳State")]
    private void TestGoToState()
    {
        ForceGoToState();
    }

    public virtual void OnStateEnter()
    {
    }

    protected override void OnEnterState()
    {
        base.OnEnterState();
        OnStateEnter();
    }

    protected override void OnExitState()
    {
        base.OnExitState();
        StateExitCancellationTokenSource?.Cancel();
    }

    private CancellationTokenSource StateExitCancellationTokenSource;

    public CancellationTokenSource GetStateExitCancellationTokenSource()
    {
        if (StateExitCancellationTokenSource == null)
        {
            StateExitCancellationTokenSource = new CancellationTokenSource();
        }
        else if (StateExitCancellationTokenSource.IsCancellationRequested)
        {
            StateExitCancellationTokenSource.Dispose();
            StateExitCancellationTokenSource = new CancellationTokenSource();
        }

        return StateExitCancellationTokenSource;
    }
}

/// <summary>
/// 這個如果當初有再包一層，就可以無痛轉換了？
/// </summary>
// [Searchable]
// public class GeneralState : AbstractState<GeneralState>, INodeModel, IState<GeneralState>, IGuidEntity,
//     IReferenceTarget, IDefaultSerializable, IDrawHierarchyBackGround, IDrawDetail, IActionParent
// {
//    
//     public string DrawCustomIcon => "";
//
//     //FIXME: 想要culling進來後，直接空降到某個State的時間點，什麼情境？以前是
//     public float StateDuration;
//
//     //還沒用到
//     // [DropDownRef] public GeneralState NextState;
//     public Color BackgroundColor => HierarchyResource.CurrentStateColor;
//     public bool IsFullRect => false;
//     public bool IsDrawGUIHierarchyBackground =>
//         Application.isPlaying && context && context.currentStateType == stateType;
//     // [HideInInspector] [Required] public new GeneralState stateType => this;
//
//     //FIXME: 不好用
//     [AutoChildren(false)] private IStateEnter[] _stateEnters;
//     [AutoChildren(false)] private IStateExit[] _stateExits;
//
//     [FormerlySerializedAs("enterOffsetDuration")]
//     public float EnterTimeOffset = 0;
//
//     //FIXME: node base visual scripting才需要
//     [HideInInspector] private Vector2 _position;
//
//     public Vector2 position
//     {
//         get => _position;
//         set => _position = value;
//     }
//
//     public bool CanSelfTransition = false; //有必要擋嗎？
//     [AutoParent] private GeneralFSMContext context;
//     public GeneralFSMContext Context => context;
//
//
//     //TODO: 其實不需要用list? graphView會需要嗎？
//
//
//     public bool IsCurrentPlaying
//     {
//         get
//         {
//             if (context == null || context.fsm == null)
//                 return false;
//             else
//                 return context.fsm.State == stateType;
//         }
//     }
//
//     private CancellationTokenSource StateExitCancellationTokenSource;
//
//     public CancellationTokenSource GetStateExitCancellationTokenSource()
//     {
//         if (StateExitCancellationTokenSource == null)
//         {
//             StateExitCancellationTokenSource = new CancellationTokenSource();
//         }
//         else if (StateExitCancellationTokenSource.IsCancellationRequested)
//         {
//             StateExitCancellationTokenSource.Dispose();
//             StateExitCancellationTokenSource = new CancellationTokenSource();
//         }
//
//         return StateExitCancellationTokenSource;
//     }
//
//     // public Action OnStateEnterAction;
//
//     private void OnDestroy()
//     {
//         // OnStateEnterAction = null;
//     }
//
//     //FIXME: StateEnterNode, EventNode 是不是比較好？
//     public override void OnStateEnter()
//     {
//         base.OnStateEnter();
//         
//         //FIXME:拔掉這個
//         foreach (var e in _stateEnters) e.OnStateEnter();
//
//         //最新規
//         _onStateEnter?.EventHandle();
//         //FIXME: 拔掉？用_onStateEnter替代?
//         if (actions != null)
//             foreach (var action in actions)
//                 // if (action.gameObject.activeSelf)
//                 action.OnActionEnter();
//         
//
//         // foreach (var transition in transitions)
//         // {
//         //     transition.TransitionCheck();
//         // }
// #if UNITY_EDITOR
//         EditorApplication.RepaintHierarchyWindow();
// #endif
//     }
//
//     public void SetPlaybackTime(float time)
//     {
//         statusTimer = time;
//         foreach (var action in actions) action.SetPlaybackTime(time);
//     }
//
//     public override void OnStateUpdate()
//     {
//         base.OnStateUpdate();
//         //FIXME: 和下面duplicated
//         _onStateUpdate?.EventHandle();
//         if (actions != null)
//             foreach (var action in actions)
//                 if (action.isActiveAndEnabled)
//                     action.OnActionUpdate();
//
//         foreach (var transition in transitions) transition.TransitionCheck();
//     }
//
//     public override void OnStateSimulate(float deltaTime)
//     {
//         base.OnStateSimulate(deltaTime);
//         _onStateUpdate?.EventHandle();
//         StateActionImplementation(deltaTime);
//         OnStateSimulateAction();
//         if (!StateTransitionImplementation(deltaTime))
//             OnStateSimulateTransitionCheck();
//
//
//         //FIXME: 這個怎麼處理？
//         // OnSpriteUpdate();
//     }
//
//     protected virtual void StateActionImplementation(float deltaTime)
//     {
//     }
//
//     protected virtual bool StateTransitionImplementation(float deltaTime)
//     {
//         return false;
//     }
//
//     protected void OnStateSimulateAction()
//     {
//         if (actions != null)
//             foreach (var action in actions)
//                 if (action.isActiveAndEnabled)
//                     action.OnActionUpdate();
//     }
//
//     protected void OnStateSimulateTransitionCheck()
//     {
//         foreach (var transition in transitions) transition.TransitionCheck();
//     }
//
//     public override void OnRenderUpdate() //render update, network怎麼處理？
//     {
//         base.OnRenderUpdate();
//         if (actions == null) return;
//         foreach (var action in actions)
//             // if (action.gameObject.activeSelf)
//             if (action.isActiveAndEnabled)
//                 action.OnActionSpriteUpdate();
//     }
//
//     public override void OnStateExit()
//     {
//         base.OnStateExit();
//         
//         _onStateExit?.EventHandle(); //新規？
//         foreach (var e in _stateExits) e.OnStateExit();
//
//         if (actions != null)
//             foreach (var action in actions)
//             // if (action.gameObject.activeSelf)
//             if (action.isActiveAndEnabled)
//                 action.OnActionExit();
//
//         
//         StateExitCancellationTokenSource?.Cancel();
//     }
//
//
//     [ShowInPlayMode]
//     [GUIColor(0.3f, 0.8f, 0.8f)]
//     [Button("強制跳State")]
//     private void ForceEnterState()
//     {
//         context.ChangeState(this);
//     }
//
//     public bool TransitionCheck(GeneralState toState, float timeOffset, StateTransition fromTransition)
//     {
//         if (gameObject.activeSelf == false)
//         {
//             this.Log("TransitionCheck fail isActiveAndEnabled false");
//             return false;
//         }
//
//         var fsm = context.fsm;
//
//         if (fsm.State != stateType) return false; //現在是我才能
//         if (fsm.State == toState)
//         {
//             Debug.LogError("不能自己跳自己");
//             return false; //不能自己跳自己
//         }
//
//         toState.EnterTimeOffset = timeOffset;
//         //每個地方都要call這個有點煩
//         context.SetLastTransition(fromTransition);
//         toState.SetLastTransition(fromTransition);
//
//         context.Log("[Transition] GoTo:", toState, gameObject);
//         fsm.ChangeState(toState);
//         return true;
//     }
//
//     private void SetLastTransition(StateTransition transition)
//     {
//         _lastTransition = transition;
//     }
//
//     [PreviewInInspector] private StateTransition _lastTransition;
//
//     public bool TransitionCheck(GeneralState toState) //去另一個state
//     {
//         var fsm = context.fsm;
//         if (fsm.State != stateType) return false; //現在是我才能
//         fsm.ChangeState(toState, CanSelfTransition);
//         return true;
//     }
//
//     public bool ForceGoToState()
//     {
//         var fsm = context.fsm;
//         if (fsm.State == stateType) return false; //已經是了
//         fsm.ChangeState(stateType, CanSelfTransition);
//         return true;
//     }
//
//     // [Component(typeof(AbstractStateAction))]
//     // private void AddAction()
//     // {
//     //     
//     // }
//
//     [AutoChildren]
//     [Component(AddComponentAt.Children, "[Transition]")]
//     [PreviewInInspector]
//     private StateTransition[] transitions = Array.Empty<StateTransition>();
//
//     public StateTransition[] Transitions => transitions;
//
//     public void RefreshTransitions()
//     {
//         transitions = GetComponentsInChildren<StateTransition>();
//     }
//     // private void AddTransition()
//     // {
//     //
//     // }
//
//     //FIXME: 沒有實作
//     public StateTransition AddTransition(Type transitionType)
//     {
//         var t = this.AddChildrenComponent<StateTransition>("[Transition] NewTransition");
//         // transitions.Add(t);
//         return t;
//         // return null;
//     }
// #if UNITY_EDITOR
//     // [Button("Add Delay Node")]
//     //FIXME: 很危險，可能因為切state delay還沒結束結果沒有觸發
//     public void AddDelayNode()
//     {
//         gameObject.AddChildrenComponent<DelayActionModifier>("[Delay Node]");
//     }
// #endif
//
//     private void OnValidate()
//     {
//         stateType = this;
//         // GetComponentsInChildren(true, transitions);
//     }
//
//     [CompRef] [AutoChildren(DepthOneOnly = true)]
//     private OnStateEnterHandler _onStateEnter;
//
//     [CompRef] [AutoChildren(DepthOneOnly = true)]
//     private OnStateUpdateHandler _onStateUpdate;
//
//     [CompRef] [AutoChildren(DepthOneOnly = true)]
//     private OnStateExitHandler _onStateExit;
//
//     //NOTE: 只撈一層
//     [Component(AddComponentAt.Children, "[Action]")] [AutoChildren(DepthOneOnly = true)] //[InlineEditor()]
//     private AbstractStateAction[] actions;
//
//     // [ShowInInspector]
//     public AbstractStateAction[] Actions => actions;
//
// #if UNITY_EDITOR
//     [ShowIf("@GetAnimatorPlayAction()")]
//     [Button("編輯動畫 Shift+E")]
//     private void EditClip()
//     {
//         //get interface IAnimatorPlayAction in children, and edit clip
//         // GetAnimatorPlayAction
//         animatorPlayAction?.EditClip();
//         //哭了我還不知道AnimatorPlayAction
//     }
// #endif
//     private IAnimatorPlayAction GetAnimatorPlayAction()
//     {
//         if (animatorPlayAction == null)
//             animatorPlayAction = GetComponentInChildren<IAnimatorPlayAction>();
//         return animatorPlayAction;
//     }
//
//     private IAnimatorPlayAction animatorPlayAction;
//
//     public void Pause()
//     {
//         foreach (var action in actions) action.Pause();
//     }
//
//     public void Resume()
//     {
//         foreach (var action in actions) action.Resume();
//     }
//
//
//     public void SimulationUpdate(float passedDuration)
//     {
//         statusTimer += passedDuration;
//         //FIXME: animator 播到
//         foreach (var action in actions) action.SimulationUpdate(passedDuration);
//     }
// }