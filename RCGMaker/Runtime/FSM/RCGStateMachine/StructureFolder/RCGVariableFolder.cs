using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class AbstractFolder : MonoBehaviour, IOverrideHierarchyIcon
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

public class RCGVariableFolder : AbstractFolder
{
    [ReadOnly] [Component( AddComponentAt.Children, "[Variable]")]
    public AbstractVariable flag;

    // [Component(typeof(AbstractFlag), "[Variable]")]
    // void AddComponent()
    // {
    //     //按完就沒我的事了??
    // }
    
    public void CommitVariableValues()
    {
        // var variables = GetComponentsInChildren<AbstractVariable>(true);
        foreach (var variable in variables)
        {
            variable.CommitValue();
        }
    }
    
    [AutoChildren] AbstractVariable[] variables;

    private void OnValidate()
    {
        variables = GetComponentsInChildren<AbstractVariable>(true);
        foreach (var variable in variables) variable.transform.localPosition = Vector3.zero;
    }
#if UNITY_EDITOR


    // [Button]
    public VariableBool CreateVariableBool()
    {
        var varBool = gameObject.AddChildrenComponent<VariableBool>("[Variable] flag");
        return varBool;
    }
#endif
}