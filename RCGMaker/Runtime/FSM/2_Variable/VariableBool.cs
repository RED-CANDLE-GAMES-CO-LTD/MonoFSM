using System.Collections.Generic;
using mixpanel;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
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

public interface IRebindable
{
    void SetBindingSource(IRebindable rebindable);
    void SetBindingTarget(IRebindable rebindable);
}

public class VariableBool : GenericVariable<ScriptableDataBool, FlagFieldBool, bool>, ICondition, IVariableBoolProvider,
    IBoolValue,IRebindable
{
    
    public ScriptableDataBool boolFlag =>scriptableData; // scriptableData;

    public override ScriptableDataBool ScriptableData => scriptableData == null ? boolFlag : scriptableData;
    
    [ShowInPlayMode]
    public bool FlagValue
    {
        get => CurrentValue;
        set
        {
            //FIXME: setter不該從這裡來？
            if (scriptableData && value != CurrentValue) //值有改才送事件
            {
                // Debug.Log("Variable Bool Changed " + ScriptableData.name);
                //[]: 灌tracker...   
                // _trackValue["data"] = ScriptableData.name;
                // _trackValue["value"] = value;
                //FIXME:如果要tracking要有集中管理處
                // this.Track("Variable Bool Changed", _trackValue);
            }

            // Value = value;
            SetValue(value);
            //FIXME: 這個event應該是錯的
            //ValueChangedEvent.Invoke();
        }
    }

    private readonly Value _trackValue = new();

    public bool IsValid => CurrentValue;


    [ShowInPlayMode] private Component source; //單一來源
    [ShowInPlayMode] private List<Component> overridingTargets = new(); //多個來源
    public void SetBindingTarget(IRebindable rebindable)
    {
        overridingTargets.Add(rebindable as Component);
    }
    public void SetBindingSource(IRebindable rebindable)
    {
        source = rebindable as Component;
        // Debug.Log("SetBindingSource"+source,source);
    }
}