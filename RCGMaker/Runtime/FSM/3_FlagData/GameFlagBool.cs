using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
//TODO: 用FlagFieldBool整合掉??
//TODO: ScriptableDataBool
[CreateAssetMenu(fileName = "NewBoolFlag", menuName = "GameFlag/Bool", order = 1)]
[System.Serializable]
public class GameFlagBool : AbstractScriptableData<FlagFieldBool, bool>//, IInteractableCondition
{
    // public FlagFieldBool field;
    [Button("ToggleField")]
    void ToggleField()
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
        get
        {
            return field.CurrentValue;
            // if (isTempDisabled)
            //     return false;
            // else if (GameFlagManager.Instance.TestModeFlag.TestMode == TestModeGameFlag.TestType.DeveloperStaticTest)
            //     return TestValue;
            // else
            //     return _currentValue;
        }
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
