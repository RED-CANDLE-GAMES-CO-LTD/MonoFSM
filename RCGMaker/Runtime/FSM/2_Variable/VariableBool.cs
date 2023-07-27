using System.Collections.Generic;
using mixpanel;
using RCGMaker.Core.Attributes;
// using mixpanel;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class VariableBool : VariableType<GameFlagBool, FlagFieldBool, bool>, ICondition
{
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
                // Debug.Log("Variable Bool Changed " + ScriptableData.name);
                //[]: 灌tracker...
                this.Track("ScriptableData Value Changed", new Dictionary<string, Value>()
                {
                    { "data", ScriptableData.name },
                    { "value", value }
                });
            Value = value;
            ValueChangedEvent.Invoke();
        }
    }

    public bool IsValid => Value;


    public UnityEvent ValueChangedEvent => valueChangedEvent;

    [HideInInlineEditors] public UnityEvent valueChangedEvent;
}