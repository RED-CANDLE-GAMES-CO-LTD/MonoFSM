using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGSetting;
using UnityEngine;
using Sirenix.OdinInspector;
public interface ICondition
{
    bool IsValid
    {
        get;
    }

}

public static class ConditionHelper
{
    public static bool IsAllValid(this AbstractConditionComp[] conditions)
    {
        if (conditions == null)
            return true;
        for (var i = 0; i < conditions.Length; i++)
        {
            if (conditions[i] == null)
                continue;
            if (conditions[i].gameObject.activeSelf == false) //只看自己，可能是parent有人關
                continue;
            if (conditions[i].FinalResult == false)
            {
                return false;
            }
        }
        return true;
    }
}

public static class AbstractConditionCompExtension
{
    //[]: 這麼前衛？實作放外面耶
    // public static bool IsValidCondition(this MonoBehaviour owner)
    // {
    //     AbstractConditionComp[] conditions = owner.GetComponentsInChildren<AbstractConditionComp>();
    //
    //     return conditions.IsAllValid();
    //     // for (int i = 0; i < conditions.Length; i++)
    //     // {
    //     //     if (conditions[i].FinalResult == false)
    //     //         return false;
    //     // }
    //     //
    //     // return true;
    // }
}



//FIXME: 關掉condition節點算什麼？
public abstract class AbstractConditionComp : MonoBehaviour
{
    public bool FinalResultInverted = false;
    protected abstract bool isValid { get; }

    [ShowInPlayMode]
    public bool FinalResult
    {
        get
        {
            if (Application.isPlaying == false)
                return false;
#if UNITY_EDITOR

            //Debug用，暫時強迫覆蓋值 (ex: 裝備可以在路上換)
            if (debugConditionResultOverrider != null && IsDebugMode)
                return debugConditionResultOverrider.OverrideResultValue;
#endif
            
            if (FinalResultInverted)
                return !isValid;
            else
                return isValid;
        }
    }

    
    [Component(typeof(DebugConditionResultOverrider), AddComponentAt.Children)] [AutoChildren(false)]
    private DebugConditionResultOverrider debugConditionResultOverrider;

#if UNITY_EDITOR
    [ShowIf("IsDebugMode")]
    [ShowInInspector]
    public bool OverrideValue =>
        debugConditionResultOverrider != null && debugConditionResultOverrider.OverrideResultValue;

    private static bool IsDebugMode => DebugSetting.IsDebugMode;
#endif
}

