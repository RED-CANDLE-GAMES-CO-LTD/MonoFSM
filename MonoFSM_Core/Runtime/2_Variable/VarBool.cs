using System.Collections.Generic;
using RCGExtension;
#if MIXPANEL
using mixpanel;
#endif
using RCGMaker.Core.Attributes;
using MonoFSM.Variable;
// using mixpanel;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;


// public interface IVariableBoolProvider
// {
//     bool FlagValue { get; }
//     public ScriptableDataBool ScriptableData { get; }
// }

public interface IRebindable
{
    void SetBindingSource(IRebindable rebindable);
    void SetBindingTarget(IRebindable rebindable);
}

/// <summary>
/// A MonoBehaviour representation of a boolean variable that can be bound to scriptable data.
/// This class provides functionality for boolean values that can be accessed, modified, and tracked
/// across the application.
/// </summary>
/// <remarks>
/// VarBool implements several interfaces:
/// - ICondition: Allows it to be used in conditional operations
/// - IBoolProvider: Provides boolean value accessor
/// - IRebindable: Supports runtime rebinding of data sources
/// - IDrawDetail: Supports custom drawing in the inspector
/// - IOverrideHierarchyIcon: Allows custom hierarchy icon in the Unity editor
/// </remarks>
public class VarBool : GenericMonoVariable<ScriptableDataBool, FlagFieldBool, bool>, ICondition,
    IBoolProvider, IRebindable, IDrawDetail, IOverrideHierarchyIcon
{
    public override ScriptableDataBool BindData => _bindData;

    [ShowInPlayMode]
    public bool FlagValue
    {
        get => CurrentValue;
        set
        {
            //FIXME: setter不該從這裡來？
            if (_bindData && value != CurrentValue) //值有改才送事件
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
#if MIXPANEL
    private readonly Value _trackValue = new();
#endif

    public bool IsTrue => CurrentValue;


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

    public bool IsValid => CurrentValue;
    public override GameFlagBase FinalData => _bindData;
    public bool IsFullRect { get; }
    public string IconName => "Toggle Icon"; //  "d_Toggle Icon"
    public bool IsDrawingIcon => true;
    public Texture2D CustomIcon => null;
}