using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class AbstractStateTransition : AbstractBehaviour
{
    [ValueDropdown("FindStates")]
    [Required]
    public GeneralState target;

    [ReadOnly] [ShowInInspector] public GeneralState Target => target;
    IEnumerable<GeneralState> FindStates()
    {
        return GetComponentInParent<GeneralFSMContext>().GetAllStates();
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
    GeneralState bindingState;
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

        // this.Log("[Transition] Check2:" + target.stateType, gameObject);
        var anyState = GetComponentInParent<RCGFSMAnyState>();
        if (anyState)//走any，直接過
        {
            this.Log("[Transition] GoTo:", target.stateType, gameObject);
            anyState.ForceTransition(target.stateType);
            return true;
        }
        if (bindingState == null)
            bindingState = this.GetComponentInParent<GeneralState>();


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
            // this.Log("[Transition] Fail:" + target.stateType, gameObject);
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
