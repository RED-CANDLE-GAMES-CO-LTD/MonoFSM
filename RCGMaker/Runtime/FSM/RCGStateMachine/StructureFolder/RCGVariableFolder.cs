using System;
using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class AbstractFolder : MonoBehaviour
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

public class RCGVariableFolder : AbstractFolder
{
    // [ReadOnly] [Component( AddComponentAt.Children, "[Variable]")]
    // public AbstractVariable flag;

    // [Component(typeof(AbstractFlag), "[Variable]")]
    // void AddComponent()
    // {
    //     //按完就沒我的事了??
    // }
    private void Awake()
    {
        varDict = GetVariableDict();
    }
    public AbstractVariable GetVariable(VariableTag type)
    {
        return varDict.GetValueOrDefault(type);
    }
    
    public void CommitVariableValues()
    {
        // var variables = GetComponentsInChildren<AbstractVariable>(true);
        foreach (var variable in variables)
        {
            variable.CommitValue();
        }
    }

    private Dictionary<VariableTag, AbstractVariable> varDict = new();
    Dictionary<VariableTag, AbstractVariable> GetVariableDict()
    {
        var dict = new Dictionary<VariableTag, AbstractVariable>();
        foreach (var variable in variables)
        {
            if(variable.varTag == null) continue;
                dict[variable.varTag] = variable;
        }
        return dict;
    }

  

    // [PreviewInInspector]
    [Component] [AutoChildren] private AbstractVariable[] variables = Array.Empty<AbstractVariable>();

    // private void OnValidate()
    // {
    //     variables = GetComponentsInChildren<AbstractVariable>(true);
    //     foreach (var variable in variables) variable.transform.localPosition = Vector3.zero;
    // }
#if UNITY_EDITOR


    // [Button]
    public VariableBool CreateVariableBool()
    {
        var varBool = gameObject.AddChildrenComponent<VariableBool>("[Variable] flag");
        return varBool;
    }
#endif
}