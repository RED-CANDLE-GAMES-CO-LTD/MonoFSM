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

public class VariableBool : VariableType<GameFlagBool, FlagFieldBool, bool>, ICondition
{
    protected override void Awake()
    {
        base.Awake();
        scriptableData.flagValueChangeEvent.AddListener(() =>
        {
            //FIXME: call the register...
            // scriptableData.CurrentValue
        });
    }

    // [FormerlySerializedAs("boolFlag")]
    [ReadOnly] [HideInInlineEditors] [Header("Deprecated=>scriptableData")]
    public GameFlagBool boolFlag; // scriptableData;

    public override GameFlagBool ScriptableData => scriptableData == null ? boolFlag : scriptableData;

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
            if (scriptableData && value != Value) //值有改才送事件
            {
                // Debug.Log("Variable Bool Changed " + ScriptableData.name);
                //[]: 灌tracker...   
                _trackValue["data"] = ScriptableData.name;
                _trackValue["value"] = value;
                Mixpanel.Track("ScriptableData Value Changed", _trackValue);
            }

            Value = value;
            ValueChangedEvent.Invoke();
        }
    }

    private readonly Value _trackValue = new();

    public bool IsValid => Value;


    //FIXME: 這個是錯的，要改成用scriptableData的
    public UnityEvent ValueChangedEvent => valueChangedEvent;

    [HideInInlineEditors] public UnityEvent valueChangedEvent;
}