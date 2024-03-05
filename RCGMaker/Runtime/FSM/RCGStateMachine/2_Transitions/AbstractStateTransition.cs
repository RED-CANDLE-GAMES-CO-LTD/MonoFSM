
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IState<in TState>
{
    GeneralFSMContext Context { get; }
    bool TransitionCheck(TState toState, float timeOffset, AbstractStateTransition fromTransition = null);

    bool TransitionCheck(TState toState);
    // bool ForceTransition(GeneralState stateType);
    // bool TransitionCheck(GeneralState stateType);
}


public class AbstractStateTransition : AbstractBehaviour, IGuidEntity, IDefaultSerializable
{
    [Button("依照Behaviour改名字")]
    void RenameByBehaviour()
    {
        gameObject.name = GetNameByBehaviour();
    }

    protected virtual string GetNameByBehaviour()
    {
        return "[Transition]" + target.stateType.name;
    }
    
    [ValueDropdown(nameof(FindStates))]
    [Required]
    public GeneralState target;

    // private void OnValidate()
    // {
    //     if (this.isActiveAndEnabled && target == null)
    //         Debug.LogError("No Target! 選一個", gameObject);
    // }

    [ReadOnly] [ShowInInspector] public GeneralState Target => target;
    IEnumerable<GeneralState> FindStates()
    {
        return GetComponentInParent<GeneralFSMContext>(true).GetAllStates();
        // return GetComponentInParent<GeneralFSMContext>().GetAllStates()
        //     .Where(state => state != this.GetComponentInParent<GeneralState>());
    }
    [AutoChildren(false)] AbstractConditionComp[] conditions;

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

    // [PreviewInInspector]
    [AutoParent()] private IState<GeneralState> parentState;
    public IState<GeneralState> ParentState => parentState;
    [ShowInInspector] private bool IsSelfTransition => parentState as GeneralState == target;

    
    [AutoChildren(false)]
    private ISkippableAnimationTransition[] _skippableAnimationTransitions;

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
    private bool IsSelfTransitionNotValid => target != null && IsSelfTransition && !target.CanSelfTransition;

    public bool TransitionConditionValid()
    {
        if (conditions != null && conditions.IsAllValid() == false)
            return false;

        return true;
    }

    [AutoParent] private RCGCullingGroup _cullingGroup;
    
    public bool TransitionCheck(float timeOffset = 0)
    {

        // this.Log("[Transition] Check1" + target.stateType, gameObject);
        //Transition 被關了
        //if (this.isActiveAndEnabled == false) 

        if (gameObject.activeSelf == false) //關著也想change state
        {
            // this.Log("[Transition] Check1 fail active false" + target.stateType, gameObject);
            return false;
        }

        //整顆單位關著，表示config沒有想要打開
        //FIXME:只是為了擋掉關著的FSM?
        if (_cullingGroup && _cullingGroup.HasActivated == false) 
        {
            return false;
        }
        
        if (conditions != null && conditions.IsAllValid() == false)
            return false;

        //TODO: 這個runtime拿蠻不好的, 改成通通拿IState? 合併anyState和State
        // var anyState = GetComponentInParent<IState<GeneralState>>();
        //任何東西都是iState吧？不用分了
        if (parentState != null) //走any，直接過
        {
            this.Log("[Transition] AnyState GoTo:", target.stateType, gameObject);
            if (target.stateType.gameObject.activeSelf == false)
            {
                this.Log("[Transition] Fail ChangeState target inactive" + target.stateType, gameObject);
                return false;
            }

            if (parentState.TransitionCheck(target.stateType, timeOffset, this))
            {
                //FIXME: 這個時間點會太晚嗎？ 會，這個回來就已經切到另一個state了
                //會...
                // parentState.Context.SetLastTransition(this);
            }
            return true;
        }

        if (parentState == null)
            Debug.LogError("Why no parent State" + parentState, gameObject);

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
            if (parentState == null)
                return false;
            return parentState.Context.LastTransition == this;
        }
    }

}
public abstract class AbstractBehaviour : MonoBehaviour
{
    protected virtual void Awake()
    {

    }
    protected virtual void Start()
    {

    }
}
