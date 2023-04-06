using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using RCGMaker.Core;
public interface INodeModel
{
    public Vector2 position { get; set; }

}

public class GeneralState : AbstractState<GeneralState>, INodeModel
{

    [FormerlySerializedAs("enterOffsetDuration")] public float EnterTimeOffset = 0;
    
    [HideInInspector]
    public Vector2 _position;
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

    public bool CanSelfTransition = false;
    [AutoParent] GeneralFSMContext context;
    public GeneralFSMContext Context => context;
    public List<AbstractStateTransition> transitions;
    List<AbstractStateAction> actions;
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
    private void Awake()
    {
        context = GetComponentInParent<GeneralFSMContext>();
        actions = new List<AbstractStateAction>();
        GetComponentsInChildren<AbstractStateAction>(actions);
    }


    public override void OnStateEnter()
    {
        base.OnStateEnter();
#if UNITY_EDITOR
        EditorApplication.RepaintHierarchyWindow();
#endif
        foreach (var action in actions)
        {
            action.OnActionEnter();
        }
    }
    public override void OnStateUpdate()
    {
        base.OnStateUpdate();
        foreach (var action in actions)
        {
            action.OnActionUpdate();
        }
    }
    public override void OnSpriteUpdate()
    {
        base.OnSpriteUpdate();
        foreach (var action in actions)
        {
            action.OnActionSpriteUpdate();
        }
    }
    public override void OnStateExit()
    {
        base.OnStateExit();
        foreach (var action in actions)
        {
            action.OnActionExit();
        }
    }

    [Button("強制跳State")]
    void ForceEnterState()
    {
        context.ChangeState(this);
    }
    public bool TransitionCheck(GeneralState toState,float timeOffset)
    {
        var fsm = context.fsm;
 
        if (fsm.State == stateType) //現在是我才能
        {
            toState.EnterTimeOffset = timeOffset;
            fsm.ChangeState(toState, CanSelfTransition);

            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    [Component(typeof(AbstractStateAction))]
    private void AddAction()
    {
        
    }

    [Component(typeof(AbstractStateTransition))]
    private void AddTransition()
    {
        
    }

    public AbstractStateTransition AddTransition(System.Type transitionType)
    {
        var t = this.AddChildrenComponent<AbstractStateTransition>(transitionType, "[Transition] NewTransition");
        transitions.Add(t);
        return t;
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
    [Button("Add Delay Node")]
    public void AddDelayNode()
    {
        gameObject.AddChildrenComponent<DelayActionModifier>("[Delay Node]");
    }
    
    private void OnValidate()
    {
        stateType = this;
        GetComponentsInChildren<AbstractStateTransition>(true, transitions);
        // transitions.RemoveAllNull();
    }

    // #if UNITY_EDITOR
    // [ReadOnly]
    // [Component(typeof(AbstractStateAction), "[Action]")]
    // // #endif
    // public AbstractStateAction testAction;

    [Component(typeof(AbstractStateAction), AddComponentAt.Children, "[Action]")]
    void AddInpsectorFunc()
    {

    }

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


#endif
}
