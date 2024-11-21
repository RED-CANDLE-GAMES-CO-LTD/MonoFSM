using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using RCGMaker.Core;
using RCGMaker.Core.Attributes;

public interface INodeModel
{
    public Vector2 position { get; set; }

}
public interface IStateEnter
{
   void OnStateEnter();
}

public interface IStateExit
{
   void OnStateExit();
}

public interface IGuidEntity
{
}

public interface ISerializableComponent
{
    public string Serialize();
    public void Deserialize(string data);
}

public interface IDefaultSerializable
{
}

[Searchable]
public class GeneralState : AbstractState<GeneralState>, INodeModel, IState<GeneralState>, IGuidEntity,
    IDefaultSerializable, IDrawHierarchyBackGround, IDrawDetail
{
    public Color BackgroundColor => HierarchyResource.CurrentStateColor;
    public bool IsFullRect => false;
    public string DrawCustomIcon => "";

    public bool IsDrawGUIHierarchyBackground =>
        Application.isPlaying && context && context.currentStateType == stateType;
    // [HideInInspector] [Required] public new GeneralState stateType => this;

    [AutoChildren(false)] private IStateEnter[] _stateEnters;
    [AutoChildren(false)] private IStateExit[] _stateExits;

    [FormerlySerializedAs("enterOffsetDuration")] public float EnterTimeOffset = 0;

    [HideInInspector] Vector2 _position;
    public Vector2 position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
        }
    }

    public bool CanSelfTransition = false; //有必要擋嗎？
    [AutoParent] GeneralFSMContext context;
    public GeneralFSMContext Context => context;

   
    //TODO: 其實不需要用list? graphView會需要嗎？


    public bool IsCurrentPlaying
    {
        get
        {
            if (context == null || context.fsm == null)
                return false;
            else
                return context.fsm.State == stateType;
        }
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

    public Action OnStateEnterAction;

    private void OnDestroy()
    {
        OnStateEnterAction = null;
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        // Debug.Log("OnStateEnter");
        OnStateEnterAction?.Invoke();
        
        foreach (var e in _stateEnters)
        {
            e.OnStateEnter();
        }
#if UNITY_EDITOR
        EditorApplication.RepaintHierarchyWindow();
#endif
        if (actions == null) return;
        foreach (var action in actions)
        {
            if (action.isActiveAndEnabled)
            // if (action.gameObject.activeSelf)
                action.OnActionEnter();
        }


       
    }

    public void SetPlaybackTime(float time)
    {
        statusTimer = time;
        foreach (var action in actions)
        {
            action.SetPlaybackTime(time);
        }
    }
    
    public override void OnStateUpdate()
    {
        base.OnStateUpdate();
        //
         if (actions == null) return;
         
         //不明原因曾經是反過來叫的。
        // for (var index = actions.Length - 1; index >= 0; index--)
        // {
        //     var action = actions[index];
        //     if (action.isActiveAndEnabled)
        //     // if (action.gameObject.activeSelf)
        //         action.OnActionUpdate();
        // }
        //
        
        foreach (var action in actions)
        {
            if (action.isActiveAndEnabled)
                action.OnActionUpdate();
        }
    }

    public override void OnSpriteUpdate()
    {
        base.OnSpriteUpdate();
        if (actions == null) return;
        foreach (var action in actions)
        {
            // if (action.gameObject.activeSelf)
            if (action.isActiveAndEnabled)
                action.OnActionSpriteUpdate();
        }
    }
    public override void OnStateExit()
    {
        base.OnStateExit();
      
        foreach (var e in _stateExits)
        {
            e.OnStateExit();
        }
        
        if (actions == null) return;
        foreach (var action in actions)
        {
            // if (action.gameObject.activeSelf)
            if (action.isActiveAndEnabled)
                action.OnActionExit();
        }
        
   

        StateExitCancellationTokenSource?.Cancel();
    }

  
    [ShowInPlayMode]
    [GUIColor(0.3f, 0.8f, 0.8f)]
    [Button("強制跳State")]
    void ForceEnterState()
    {
        context.ChangeState(this);
    }

    public bool TransitionCheck(GeneralState toState, float timeOffset, AbstractStateTransition fromTransition)
    {
        if (gameObject.activeSelf == false)
        {
            this.Log("TransitionCheck fail isActiveAndEnabled false");
            return false;
        }

        var fsm = context.fsm;

        if (fsm.State == stateType) //現在是我才能
        {
            toState.EnterTimeOffset = timeOffset;
            //每個地方都要call這個有點煩
            context.SetLastTransition(fromTransition);
            fsm.ChangeState(toState, CanSelfTransition);

            return true;
        }
        return false;
    }

    public bool TransitionCheck(GeneralState toState)
    {
        var fsm = context.fsm;
        if (fsm.State != stateType) return false; //現在是我才能
        fsm.ChangeState(toState, CanSelfTransition);
        return true;
    }


    // [Component(typeof(AbstractStateAction))]
    // private void AddAction()
    // {
    //     
    // }

    [AutoChildren]
    [Component( AddComponentAt.Children, "[Transition]")]
    // [InlineEditor()]
    [PreviewInInspector]
    AbstractStateTransition[] transitions;

    public AbstractStateTransition[] Transitions => transitions;

    public void RefreshTransitions()
    {
        transitions = GetComponentsInChildren<AbstractStateTransition>();
    }
    // private void AddTransition()
    // {
    //
    // }

    //FIXME: 沒有實作
    public AbstractStateTransition AddTransition(System.Type transitionType)
    {
        var t = this.AddChildrenComponent<AbstractStateTransition>("[Transition] NewTransition");
        // transitions.Add(t);
        return t;
        // return null;
    }
    // [Button("Add Animator Play")]
    // void AddAnimatorPlay()
    // {
    //     Undo.AddComponent(gameObject, typeof(AnimatorPlayAction));
    // }
    //
    // [Button("Add Event Transition")]
    // public void AddEventTransitionEditor()
    // {
    //     AddEventTransition();
    // }

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
#if UNITY_EDITOR
    // [Button("Add Delay Node")]
    //FIXME: 很危險，可能因為切state delay還沒結束結果沒有觸發
    public void AddDelayNode()
    {
        gameObject.AddChildrenComponent<DelayActionModifier>("[Delay Node]");
    }
    #endif

    private void OnValidate()
    {
        stateType = this;
        // GetComponentsInChildren(true, transitions);
        
    }

    // #if UNITY_EDITOR
    // [ReadOnly]
    // [Component(typeof(AbstractStateAction), "[Action]")]
    // // #endif
    // public AbstractStateAction testAction;
    
    //NOTE: 只撈一層
    [Component(AddComponentAt.Children, "[Action]")] [AutoChildren(DepthOneOnly = true)] //[InlineEditor()]
    private AbstractStateAction[] actions;

    // [ShowInInspector]
    public AbstractStateAction[] Actions => actions;
    // #if UNITY_EDITOR
    //     [Component(typeof(AbstractStateAction), "[Action]")]
    // #endif
    //     public List<AbstractStateAction> testActions;

    // private int CustomAddFunction()
    // {
    //     Debug.Log("Custom Add");
    //     return this.testActions.Count;
    // }
    // // [ReadOnly]
    // [ListDrawerSettings(CustomAddFunction = "CustomAddFunction")]
    // [Mono(typeof(AbstractAction), "[Action]")]
    // public List<AbstractAction> testActions;

    // // [MovedFrom("PlayerTestState")]
    // [SerializeReference]
    // ICommand command;
#if UNITY_EDITOR
    [ShowIf("@GetAnimatorPlayAction()")]
    [Button("編輯動畫 Shift+E")]
    private void EditClip()
    {
        //get interface IAnimatorPlayAction in children, and edit clip
        // GetAnimatorPlayAction
        animatorPlayAction?.EditClip();
        //哭了我還不知道AnimatorPlayAction
    }
#endif
    private IAnimatorPlayAction GetAnimatorPlayAction()
    {
        if (animatorPlayAction == null)
            animatorPlayAction = GetComponentInChildren<IAnimatorPlayAction>();
        return animatorPlayAction;
    }

    private IAnimatorPlayAction animatorPlayAction;

    public void Pause()
    {
        foreach (var action in actions)
        {
            action.Pause();
        }
    }

    public void Resume()
    {
        foreach (var action in actions)
        {
            action.Resume();
        }
    }

   
}
