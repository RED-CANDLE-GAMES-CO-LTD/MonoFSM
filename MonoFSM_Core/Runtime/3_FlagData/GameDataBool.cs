using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

//TODO: 用FlagFieldBool整合掉??
//TODO: ScriptableDataBool
[Serializable]
public class DataBoolModifyEntryTest : DataModifyEntry<GameDataBool, FlagFieldBool, bool>
{
}

[Serializable]
public abstract class DataModifyEntry<TData, TField, T> : BaseDataModifyEntry
    where T : struct where TData : AbstractScriptableData<TField, T> where TField : FlagField<T>
{
    public TData Data;
    public T targetValue;
    [ReadOnly] public T oriValue;
    [ShowInInspector] public string Note => Data.Note;

    public void Apply()
    {
        oriValue = Data.CurrentValue;
        Data.CurrentValue = targetValue;
    }

    public void Revert()
    {
        Data.CurrentValue = oriValue;
    }
}

[Serializable]
public abstract class BaseDataModifyEntry //沒什麼用... 不利於refactor
{
}

[CreateAssetMenu(fileName = "ScriptableDataBool", menuName = "ScriptableData/Bool", order = 1)]
[Serializable]
public class GameDataBool : AbstractScriptableData<FlagFieldBool, bool> //, IInteractableCondition
{
    // public FlagFieldBool field;
    [Button("ToggleField")]
    private void ToggleField()
    {
        field.CurrentValue = !field.CurrentValue;
    }
    // [Header("Game Setting")]
    // public bool DefaultValue;
    // public bool TestValue = true;

    // [Header("Current State")]
    // [SerializeField]
    // private bool _currentValue;
    //FIXME: 暫時關掉某個flag!?
    // public bool isTempDisabled;

    public UnityEvent flagValueChangeEvent;

    // public void DisableForDuration(float seconds = 0.5f)
    // {
    //     isTempDisabled = true;
    //     Timer.AddTask(() =>
    //     {
    //         isTempDisabled = false;
    //     }, seconds);
    // }
    public bool CurrentValue
    {
        //TODO: refactor with flag field
        get => field.CurrentValue;
        // if (isTempDisabled)
        //     return false;
        // else if (GameFlagManager.Instance.TestModeFlag.TestMode == TestModeGameFlag.TestType.DeveloperStaticTest)
        //     return TestValue;
        // else
        //     return _currentValue;
        set
        {
            // _currentValue = value;

            field.CurrentValue = value;
            if (flagValueChangeEvent != null)
                flagValueChangeEvent.Invoke();
        }
    }

    public bool isValid => CurrentValue;

    public override void FlagAwake(TestMode mode)
    {
        base.FlagAwake(mode);
        // isTempDisabled = false;
        //         _currentValue = DefaultValue;
        // #if UNITY_EDITOR
        //         if (field.DefaultValue != DefaultValue)
        //         {
        //             field.DefaultValue = DefaultValue;
        //             UnityEditor.EditorUtility.SetDirty(this);
        //         }
        // #endif
        // if (GameFlagManager.Instance.isTestMode)
        //     _currentValue = TestValue;
    }
}