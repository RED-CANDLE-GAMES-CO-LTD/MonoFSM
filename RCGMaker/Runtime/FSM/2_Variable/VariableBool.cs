using System.Collections.Generic;
using mixpanel;
using RCGMaker.Core.Attributes;
// using mixpanel;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

internal interface IValueChangeCallback
{
    void OnValueChanged(bool value);
}

public interface IVariableBoolProvider
{
    bool FlagValue { get; }
    public ScriptableDataBool ScriptableData { get; }
}

public class VariableBool : VariableType<ScriptableDataBool, FlagFieldBool, bool>, ICondition, IVariableBoolProvider
{
    // protected override void Awake()
    // {
    //     base.Awake();
    //     scriptableData.flagValueChangeEvent.AddListener(() =>
    //     {
    //         //FIXME: call the register...
    //         // scriptableData.CurrentValue
    //     });
    // }

    // [FormerlySerializedAs("boolFlag")]
    [ReadOnly] [HideInInlineEditors] [Header("Deprecated=>scriptableData")]
    public ScriptableDataBool boolFlag; // scriptableData;

    public override ScriptableDataBool ScriptableData => scriptableData == null ? boolFlag : scriptableData;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (scriptableData == null && boolFlag != null)
            scriptableData = boolFlag;
        //資料夾裡面的放在 0 0 0
    }

    // [FormerlySerializedAs("tempFlag")]
    // public FlagFieldBool localField;// localField;
    [ShowInPlayMode]
    public bool FlagValue
    {
        get => Value;
        set
        {
            //FIXME: setter不該從這裡來？
            if (scriptableData && value != Value) //值有改才送事件
            {
                // Debug.Log("Variable Bool Changed " + ScriptableData.name);
                //[]: 灌tracker...   
                _trackValue["data"] = ScriptableData.name;
                _trackValue["value"] = value;
                //FIXME:如果要tracking要有集中管理處
                // Mixpanel.Track("ScriptableData Value Changed", _trackValue);
            }

            // Value = value;
            SetValue(value);
            //FIXME: 這個event應該是錯的
            ValueChangedEvent.Invoke();
        }
    }

    private readonly Value _trackValue = new();

    public bool IsValid => Value;


}