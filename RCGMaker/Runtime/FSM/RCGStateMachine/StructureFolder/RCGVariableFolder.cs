using System;
using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class AbstractFolder : MonoBehaviour
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

public class RCGVariableFolder : MonoDict<VariableTag, AbstractMonoVariable>
{
    // [ReadOnly] [Component( AddComponentAt.Children, "[Variable]")]
    // public AbstractVariable flag;

    // [Component(typeof(AbstractFlag), "[Variable]")]
    // void AddComponent()
    // {
    //     //按完就沒我的事了??
    // }
    // private void Awake()
    // {
    //     // varDict = GetVariableDict();
    // }
    public AbstractMonoVariable GetVariable(VariableTag type)
    {
        return Get(type);
        // return varDict.GetValueOrDefault(type);
    }

    public AbstractMonoVariable GetVariable(string varName)
    {
        return Get(varName);
    }

    //GetConfig?

    public void CommitVariableValues()
    {
        // var variables = GetComponentsInChildren<AbstractVariable>(true);
        foreach (var variable in _variables)
        {
            variable.CommitValue();
        }
    }

    // private Dictionary<VariableTag, AbstractVariable> varDict = new();
    // Dictionary<VariableTag, AbstractVariable> GetVariableDict()
    // {
    //     var dict = new Dictionary<VariableTag, AbstractVariable>();
    //     foreach (var variable in variables)
    //     {
    //         if (variable is AbstractVariable abstractVariable)
    //         {
    //             if(abstractVariable.varTag == null) continue;
    //             dict[abstractVariable.varTag] = abstractVariable;
    //         }
    //             
    //         // if(variable.varTag == null) continue;
    //         //     dict[variable.varTag] = variable;
    //     }
    //     return dict;
    // }


    // [PreviewInInspector]
    [PreviewInInspector] [Component] [AutoChildren]
    private ISettable[] _variables = Array.Empty<ISettable>();

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
    protected override void RemoveImplement(AbstractMonoVariable item)
    {
    }

    protected override bool CanBeAdded(AbstractMonoVariable item)
    {
        return item.gameObject.activeSelf == true;
        //一定要可以加，還是用disable?
        // return true;
    }
}