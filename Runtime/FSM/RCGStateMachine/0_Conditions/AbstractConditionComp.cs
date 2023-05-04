using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
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
            if (conditions[i].isActiveAndEnabled == false)
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
    public static bool IsValidCondition(this MonoBehaviour owner)
    {
        AbstractConditionComp[] conditions = owner.GetComponentsInChildren<AbstractConditionComp>();

        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i].FinalResult == false)
                return false;
        }

        return true;
    }
}



//FIXME: 關掉condition節點算什麼？
public abstract class AbstractConditionComp : MonoBehaviour
{
    public bool FinalResultInverted = false;
    protected abstract bool isValid { get; }

    [RuntimeDisplay]
    public bool FinalResult
    {
        get
        {
            if (Application.isPlaying == false)
                return false;

            if (FinalResultInverted)
                return !isValid;
            else
                return isValid;
        }
    }
}
