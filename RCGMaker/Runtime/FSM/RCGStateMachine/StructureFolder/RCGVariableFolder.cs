using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RCGVariableFolder : MonoBehaviour
{
    [ReadOnly] [Component(typeof(AbstractVariable), AddComponentAt.Children, "[Variable]")]
    public AbstractVariable flag;

    // [Component(typeof(AbstractFlag), "[Variable]")]
    // void AddComponent()
    // {
    //     //按完就沒我的事了??
    // }

    private void OnValidate()
    {
        var variables = GetComponentsInChildren<AbstractVariable>(true);
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