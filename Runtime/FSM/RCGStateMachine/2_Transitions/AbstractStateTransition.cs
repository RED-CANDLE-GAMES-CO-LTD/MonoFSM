using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IState<in TState>
{
    bool TransitionCheck(TState toState, float timeOffset);

    bool TransitionCheck(TState toState);
    // bool ForceTransition(GeneralState stateType);
    // bool TransitionCheck(GeneralState stateType);
}
public class AbstractStateTransition : AbstractBehaviour
{
    [ValueDropdown("FindStates")]
    [Required]
    public GeneralState target;

    [ReadOnly] [ShowInInspector] public GeneralState Target => target;
    IEnumerable<GeneralState> FindStates()
    {
        return GetComponentInParent<GeneralFSMContext>(true).GetAllStates();
        // return GetComponentInParent<GeneralFSMContext>().GetAllStates()
        //     .Where(state => state != this.GetComponentInParent<GeneralState>());
    }
    [AutoChildren(false)] AbstractConditionComp[] conditions;
    // protected override void Awake()
    // {
    //     bindingState = GetComponentInParent<GeneralState>();
    // }
    [Button("測試transition")]
    void TransitionTest()
    {
        TransitionCheck(0);
    }

    [AutoParent()] private GeneralState bindingState;
    [AutoParent()] private IState<GeneralState> parentState;
    public bool TransitionCheck(float timeOffset=0)
    {

        // this.Log("[Transition] Check1" + target.stateType, gameObject);
        //Transition 被關了
        if (this.gameObject.activeInHierarchy == false)
        {
            // this.Log("[Transition] Check1 fail active false" + target.stateType, gameObject);
            return false;
        }
        if (conditions.IsAllValid() == false)
            return false;

        //TODO: 這個runtime拿蠻不好的, 改成通通拿IState? 合併anyState和State
        var anyState = GetComponentInParent<IState<GeneralState>>();
        if (anyState != null) //走any，直接過
        {
            this.Log("[Transition] GoTo:", target.stateType, gameObject);
            if (target.stateType.isActiveAndEnabled == false)
            {
                this.Log("[Transition] Fail ChangeState target inactive" + target.stateType, gameObject);
                return false;
            }
            anyState.TransitionCheck(target.stateType, timeOffset);
            return true;
        }

        if (parentState == null)
            Debug.LogError("Why no parent State" + anyState, gameObject);

        if (parentState.TransitionCheck(target, timeOffset))
        {
            
        }
        
        if (bindingState == null)
            Debug.LogError("Why no parent State" + anyState, gameObject);

     
        // this.Log("[Transition] Check3:" + target.stateType, gameObject);
        //好像不該回頭做？可是要不然要怎麼辦...不能用事件接
        
        if (bindingState.TransitionCheck(target.stateType,timeOffset))
        {
            bindingState.Context.lastTransition = this;
            this.Log("[Transition] GoTo:", target.stateType, gameObject);
            // #if UNITY_EDITOR
            //             UnityEditor.EditorGUIUtility.PingObject(gameObject);
            // #endif
            return true;
        }
        else
        {
            this.Log("[Transition] Fail:" + target.stateType, gameObject);
        }
        return false;
    }
    public bool IsLastTransition
    {
        get
        {
            if (bindingState == null)
                return false;
            return bindingState.Context.lastTransition == this;
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
