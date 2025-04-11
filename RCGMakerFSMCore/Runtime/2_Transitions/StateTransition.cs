using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using jerryee.UnityMCP;
using MonoFSM.Foundation;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public interface IState<in TState>
{
    GeneralFSMContext Context { get; }
    bool TransitionCheck(TState toState, float timeOffset, StateTransition fromTransition = null);

    bool TransitionCheck(TState toState);
    // bool ForceTransition(GeneralState stateType);
    // bool TransitionCheck(GeneralState stateType);
}

//FIXME:如果所有的condition都可以自行註冊，這個就不需要了，全部都用condition處理
public interface ITransitionCheckInvoker //interface沒有意義？
{
    // void RegisterTransitionCheck(ITransitionCheckingTarget target);
    // void UnRegisterTransitionCheck(ITransitionCheckingTarget target);
    // ITransitionCheckingTarget TransitionTarget { get; }
}

// public interface ITransitionCheckingTarget
// {
//     bool OnTransitionCheck();
// }

//還是用IRCGEventReceiver?

[Searchable]
public class StateTransition : AbstractDescriptionBehaviour, IGuidEntity, IDefaultSerializable, IResetStateRestore,IBeforePrefabSaveCallbackReceiver
{
    //現在event driven直接set也work, 要做成只有condition改變才會觸發transition?
    public bool IsTransitionCheckNeeded = false;
    //TODO: 我需要保證附近有checker, 我應該和_checker註冊？
    //FIXME: 空transition就可以自動過去？
    [InfoBox("No Checker", InfoMessageType.Error, nameof(NoChecker))]
    [PreviewInInspector] [AutoParent] [Component(AddComponentAt.Same)] //FIXME: 會有需要parent的情況嗎？ children也包括自己
    ITransitionCheckInvoker _checkInvoker; 

    //要分同層級的嗎？
    [Component]
    [PreviewInInspector][AutoChildren] ITransitionCheckInvoker[] _childrenCheckers = Array.Empty<ITransitionCheckInvoker>();

    bool NoChecker => !HasChecker();
    bool HasChecker()
    {
        return _checkInvoker != null || _childrenCheckers is { Length: > 0 };
    }

    [Button("依照Behaviour改名字")]
    void RenameByBehaviour()
    {
        gameObject.name = GetNameByBehaviour();
    }

    protected virtual string GetNameByBehaviour()
    {
        return "[Transition] =>" + _target.stateType.name.Replace("[State]", "");
    }

    bool TransitionValidationResult()
    {
        if (_target == _parentState as GeneralState)
            return true;
        return false;
    }

    //這個其實光是用名字就可以了耶？
    [FormerlySerializedAs("target")]
    [MCPExtractable]
    [InfoBox("Target is self", InfoMessageType.Error, nameof(TransitionValidationResult))]
    [ValueDropdown(nameof(FindStates),NumberOfItemsBeforeEnablingSearch=5)]
    [Required]
    [Header("Go To")]
    [GUIColor(0.8f, 0.8f, 1)]
    [SerializeField]
    protected GeneralState _target;

    // private void OnValidate()
    // {
    //     if (this.isActiveAndEnabled && target == null)
    //         Debug.LogError("No Target! 選一個", gameObject);
    // }

    [ReadOnly] [ShowInInspector] public GeneralState Target => _target;

    IEnumerable<GeneralState> FindStates()
    {
        return GetComponentInParent<GeneralFSMContext>(true).GetAllGeneralStates();
        // return GetComponentInParent<GeneralFSMContext>().GetAllStates()
        //     .Where(state => state != this.GetComponentInParent<GeneralState>());
    }

    [PreviewInInspector] [AutoChildren(false)] [Component]
    private AbstractConditionComp[] conditions = Array.Empty<AbstractConditionComp>();

    [Title("從init來會播動畫的Transition")]
    [ShowInInspector]
    public bool IsDefaultTransition => conditions == null || conditions.Length == 0;
    //試圖封裝 resolving和resolved，不想要把clip和transition分開，有隱含邏輯在裡面

    // protected override void Awake()
    // {
    //     bindingState = GetComponentInParent<GeneralState>();
    // }
    [Button("測試transition")]
    void TransitionTest()
    {
        TransitionCheck(0);
    }

    // [AutoParent()] private GeneralState bindingState;

    [PreviewInInspector] [AutoParent()] private IState<GeneralState> _parentState;
    public IState<GeneralState> ParentState => _parentState;
    [ShowInInspector] private bool IsSelfTransition => _parentState as GeneralState == _target;


    [AutoChildren(false)] private ISkippableAnimationTransition[] _skippableAnimationTransitions;

    [ShowInInspector]
    public bool IsTransitionSkippable
    {
        get
        {
            if (_skippableAnimationTransitions == null)
            {
                return true;
            }

            foreach (var s in _skippableAnimationTransitions)
            {
                if (s.CanSkip() == false)
                    return false;
            }

            return true;
        }
    }


    [InfoBox("SelfTransition要勾才會過", InfoMessageType.Error, "IsSelfTransitionNotValid")]
    [ShowInInspector]
    private bool IsSelfTransitionNotValid => _target != null && IsSelfTransition && !_target.CanSelfTransition;

    [PreviewInInspector]
    public bool TransitionConditionValid
    {
        get
        {
            if (conditions != null && conditions.IsAllValid() == false)
                return false;

            return true;
        }
    }

    // [AutoParent] private RCGCullingGroup _cullingGroup;

    //FIXME: 不該空降call, 只能在系統特定時間點
    public bool TransitionCheck(float timeOffset = 0)
    {
        if(IsTransitionCheckNeeded == false)
            return false;
        // this.Log("[Transition] Check1" + target.stateType, gameObject);
        //Transition 被關了
        //if (this.isActiveAndEnabled == false) 
        IsTransitionCheckNeeded = false;
        if (gameObject.activeSelf == false) //關著也想change state
        {
            // this.Log("[Transition] Check1 fail active false" + target.stateType, gameObject);
            return false;
        }

        //整顆單位關著，表示config沒有想要打開
        //FIXME:只是為了擋掉關著的FSM?
        // if (_cullingGroup && _cullingGroup.HasActivated == false)
        // {
        //     return false;
        // }

        if (conditions != null && conditions.IsAllValid() == false)
            return false;

        //TODO: 這個runtime拿蠻不好的, 改成通通拿IState? 合併anyState和State
        // var anyState = GetComponentInParent<IState<GeneralState>>();
        //任何東西都是iState吧？不用分了
        if (_parentState != null) //走any，直接過
        {
            if (_target == null)
            {
                Debug.LogError("No Target! 選一個", gameObject);
                return false;
            }

            
            if (_target.stateType.gameObject.activeSelf == false)
            {
                this.Log("[Transition] Fail ChangeState target inactive" + _target.stateType, gameObject);
                return false;
            }

            if (_parentState.TransitionCheck(_target.stateType, timeOffset, this))
            {
                //FIXME: 這個時間點會太晚嗎？ 會，這個回來就已經切到另一個state了
                //會...
           
                
            }

            return true;
        }

        if (_parentState == null)
            Debug.LogError("Why no parent State" + _parentState, gameObject);

        // if (parentState.TransitionCheck(target, timeOffset))
        // {
        //     
        // }
        //
        // if (bindingState == null)
        //     Debug.LogError("Why no parent State" + anyState, gameObject);


        // this.Log("[Transition] Check3:" + target.stateType, gameObject);
        //好像不該回頭做？可是要不然要怎麼辦...不能用事件接

        // if (bindingState.TransitionCheck(target.stateType,timeOffset))
        // {
        //     bindingState.Context.lastTransition = this;
        //     this.Log("[Transition] GoTo:", target.stateType, gameObject);
        //     // #if UNITY_EDITOR
        //     //             UnityEditor.EditorGUIUtility.PingObject(gameObject);
        //     // #endif
        //     return true;
        // }
        // else
        // {
        //     this.Log("[Transition] Fail:" + target.stateType, gameObject);
        // }
        return false;
    }

    public bool IsLastTransition
    {
        get
        {
            if (_parentState == null)
                return false;
            return _parentState.Context.LastTransition == this;
        }
    }

    public void ResetStateRestore()
    {
        if (!HasChecker())
            Debug.LogError("No Checker", gameObject);
    }

    //需要外部通知檢查Transition, Update / ValueChanged, 還有嗎？
    public void OnBeforePrefabSave()
    {
        RenameByBehaviour();
    }

    protected override string DescriptionTag => "Transition";
}
