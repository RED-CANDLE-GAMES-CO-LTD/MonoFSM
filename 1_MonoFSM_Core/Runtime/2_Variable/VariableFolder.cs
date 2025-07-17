using System;

using UnityEngine;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;

public abstract class AbstractFolder : MonoBehaviour
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

//FIXME: 這個才該叫做blackboard?，這個是用來放變數的?

public class VariableFolder : MonoDict<VariableTag, AbstractMonoVariable>
{
    protected override bool IsStringDictEnable => true;
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

    public TVariable GetVariable<TVariable>(VariableTag type) where TVariable : AbstractMonoVariable
    {
        return Get(type) as TVariable;
    }

    public TVariable GetVariable<TVariable>(string varName) where TVariable : AbstractMonoVariable
    {
        return Get(varName) as TVariable;
    }

    //GetConfig?

    public void CommitVariableValues()
    {
        // var variables = GetComponentsInChildren<AbstractVariable>(true);
        //FIXME: 用

        foreach (var variable in _collections)
            if (variable is ISettable settableVariable)
                settableVariable.CommitValue();
    }

    // [PreviewInInspector]
    // [PreviewInInspector] [Component] [AutoChildren]
    // private ISettable[] _variables = Array.Empty<ISettable>();

    // private void OnValidate()
    // {
    //     variables = GetComponentsInChildren<AbstractVariable>(true);
    //     foreach (var variable in variables) variable.transform.localPosition = Vector3.zero;
    // }
#if UNITY_EDITOR


    // [Button]
    public VarBool CreateVariableBool()
    {
        var varBool = gameObject.AddChildrenComponent<VarBool>("[Variable] flag");
        return varBool;
    }
#endif
    protected override void AddImplement(AbstractMonoVariable item)
    {
        
    }

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