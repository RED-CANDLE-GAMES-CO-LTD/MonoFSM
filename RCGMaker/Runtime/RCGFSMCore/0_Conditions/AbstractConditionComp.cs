using System;
using System.Collections;
using System.Collections.Generic;
using jerryee.UnityMCP;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGSetting;
using UnityEngine;
using Sirenix.OdinInspector;

public interface ICondition
{
    bool IsValid { get; }
}

public static class ConditionHelper
{
    public static bool IsAllValid(this AbstractConditionComp[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;
        foreach (var condition in conditions)
        {
            if (condition == null)
                continue;
            if (condition.gameObject.activeSelf == false) //只看自己，可能是parent有人關
                continue;
            if (condition.FinalResult == false)
            {
                return false;
            }
        }

        return true;
    }
}

//FIXME: 關掉condition節點算什麼？
public abstract class AbstractConditionComp : MonoBehaviour, IBoolValue
{
    protected virtual bool IsShowRenameButton => nameDescription != "";

    //FIXME: AI 可以解釋性？
    [MCPExtractable] protected virtual string nameDescription => this.GetType().Name;

    [Button]
    [ShowIf("IsShowRenameButton")]
    protected void RenameOfGameObject()
    {
        var text = "[Condition] " + nameDescription;
        if (FinalResultInverted)
            text += " is Inverted";
        gameObject.name = text;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    // public Action OnConditionChanged; //要用這個？還是用polling就好了
    //直接用interface往上叫好像不錯？
    private bool _isConditionChanged = false;

    //用類似statData 檢查dirty來決定要不要重新檢查condition
    public bool IsDirty => _isConditionChanged;

    public bool FinalResultInverted = false;
    protected abstract bool IsValid { get; }

    [ShowInPlayMode]
    public bool FinalResult
    {
        get
        {
            if (Application.isPlaying == false)
                return false;
#if UNITY_EDITOR

            //Debug用，暫時強迫覆蓋值 (ex: 裝備可以在路上換)
            if (_debugConditionResultOverrider != null && IsDebugMode)
                return _debugConditionResultOverrider.OverrideResultValue;
#endif
            //之前都沒有...
            // if (isActiveAndEnabled == false)
            //     return false;
            //FIXME: 關著表示不判...

            if (FinalResultInverted)
                return !IsValid;

            return IsValid;
        }
    }


#if UNITY_EDITOR
    [ShowIf("IsDebugMode")] [PropertyOrder(1)] [TabGroup("Debug")] [Component] [AutoChildren(false)]
    private DebugConditionResultOverrider _debugConditionResultOverrider;

    [ShowIf("IsDebugMode")]
    [ShowInInspector]
    [TabGroup("Debug")]
    public bool OverrideValue =>
        _debugConditionResultOverrider != null && _debugConditionResultOverrider.OverrideResultValue;

    private static bool IsDebugMode => DebugSetting.IsDebugMode;
#endif

    //For Cheat Code
    public virtual void CheatComplete()
    {
        Debug.LogError("This Condition Can't ForceSetValid");
    }

    public bool IsTrue => FinalResult;
}