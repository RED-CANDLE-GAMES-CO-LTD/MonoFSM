using System;
using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class AbstractFolder : MonoBehaviour
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

//FIXME: 這個才該叫做blackboard?，這個是用來放變數的?

public class VariableFolder : MonoDictFolder<VariableTag, AbstractMonoVariable>, IAfterSimulate
{
    // private Dictionary<VariableTag, AbstractMonoVariable> _varMap = new();

    private Dictionary<string, AbstractMonoVariable> _nameMap = new();

    private bool _initialized;

    // public override void EnterSceneAwake()
    // {
    //     base.EnterSceneAwake();
    //     // RebuildVariableMap();
    // }


    // [Button]
    // public void RebuildVariableMap()
    // {
    //     // _varMap.Clear();
    //     _nameMap.Clear();
    //     var variables = GetComponentsInChildren<AbstractMonoVariable>(true);
    //     foreach (var v in variables)
    //     {
    //         if (v == null) continue;
    //         //FIXME: 想要做很狂的GetVar系統，任何var用tag就可以拿到
    //         // // 和 collection 的 Add 路徑共用同一套判定，proxy var 不該被 GetVar 解析到
    //         // if (!IsAddValid(v)) continue;
    //         if (v._varTag != null)
    //         {
    //             _varMap.TryAdd(v._varTag, v);
    //         }
    //
    //         if (!string.IsNullOrEmpty(v.name))
    //         {
    //             // Assuming we want to look up by the variable name (e.g. "[Var] Health")
    //             // Or maybe the user logic cleans up the name.
    //             // For now, using the gameObject name or a property if available.
    //             // Assuming gameObject name for now as the key if no other ID exists.
    //             _nameMap.TryAdd(v.name, v);
    //         }
    //     }
    //
    //     _initialized = true;
    // }

    //FIXME: external dict?
    protected override bool IsStringDictEnable => true;

    protected override bool IsAddValid(AbstractMonoVariable value)
    {
        if (value.HasParentVarEntity) //這段擋掉對嗎？因為我想要宣告Train.HasPower
            return false;
        if (_dict.ContainsKey(value
                ._varTag)) //FIXME: 這裡是要丟錯誤還是覆寫？目前先丟錯誤，因為tag重複很可能是設計問題（不確定的tag對應不確定的變數），而且tag重複的話GetVar就會撈到不確定的變數了
        {
            Debug.LogError(
                $"[VariableFolder] Variable with tag '{value._varTag.name}' already exists in folder '{name}'. Please ensure each variable has a unique tag.",
                value);
            return false;
        }

        //現在很深喔，所有下面的變數包含getter都撈出來
        // if (value.HasParentVarEntity)
        //     return false;
        return true;
    }
    public AbstractMonoVariable GetVariable(VariableTag type)
    {
        // if (!_initialized) RebuildVariableMap();
        // if (type != null && _varMap.TryGetValue(type, out var v)) return v;
        return Get(type);
    }

    public AbstractMonoVariable GetVariable(string varName)
    {
        // if (!_initialized) RebuildVariableMap();
        if (!string.IsNullOrEmpty(varName) && _nameMap.TryGetValue(varName, out var v)) return v;

        var local = Get(varName);
        if (local != null) return local;

        foreach (var dict in _externalDicts)
        {
            if (dict == null) continue;
            var found = dict.Get(varName);
            if (found != null) return found;
        }

        return null;
    }

    // public void AddExternalFolder(VariableFolder folder)
    // {
    //     AddExternalDict(folder);
    // }
    //
    // public void RemoveExternalFolder(VariableFolder folder)
    // {
    //     RemoveExternalDict(folder);
    // }

    public TVariable GetVariable<TVariable>(VariableTag type)
        where TVariable : AbstractMonoVariable
    {
        var v = GetVariable(type);
        return v as TVariable;
    }

    public TVariable GetVariable<TVariable>(string varName)
        where TVariable : AbstractMonoVariable
    {
        var v = GetVariable(varName);
        return v as TVariable;
    }

    //GetConfig?

    public void CommitVariableValues()
    {
        // var variables = GetComponentsInChildren<AbstractVariable>(true);
        //FIXME: 用

        foreach (var variable in _collections)
        {
            // Profiler.BeginSample($"Commit in loop");
            if (variable.HasProxySource)
                continue;
            if (variable is ISettable settableVariable)
                settableVariable.CommitValue();
            // Profiler.EndSample();
        }

    }

    // [PreviewInInspector]
    // [PreviewInInspector] [Component] [AutoChildren]
    // private ISettable[] _variables = Array.Empty<ISettable>();

    // private void OnValidate()
    // {
    //     variables = GetComponentsInChildren<AbstractVariable>(true);
    //     foreach (var variable in variables) variable.transform.localPosition = Vector3.zero;
    // }

    #region EditorOnly
#if UNITY_EDITOR

    // [Button]
    public VarBool CreateVariableBool()
    {
        var varBool = gameObject.AddChildrenComponent<VarBool>("[Variable] flag");
        return varBool;
    }

    /// <summary>
    /// 創建指定類型的變數
    /// </summary>
    /// <typeparam name="TVariable">變數類型，必須繼承自 AbstractMonoVariable</typeparam>
    /// <param name="tagName">變數的標籤名稱</param>
    /// <returns>創建的變數實例</returns>
    public TVariable CreateVariable<TVariable>(string tagName)
        where TVariable : AbstractMonoVariable
    {
        var variable = gameObject.AddChildrenComponent<TVariable>($"[Var] {tagName}");

        // 這裡可以進一步設定 VariableTag，如果有需要的話
        // variable._varTag = FindOrCreateVariableTag(tagName);

        return variable;
    }

    /// <summary>
    /// 創建變數的通用方法
    /// </summary>
    /// <param name="variableType">變數類型</param>
    /// <param name="tagName">變數的標籤名稱</param>
    /// <returns>創建的變數實例</returns>
    public AbstractMonoVariable CreateVariable(Type variableType, string tagName)
    {
        if (!typeof(AbstractMonoVariable).IsAssignableFrom(variableType))
        {
            Debug.LogError($"類型 {variableType} 不是 AbstractMonoVariable 的子類別");
            return null;
        }

        var childGameObject = new GameObject($"[Var] {tagName}");
        childGameObject.transform.SetParent(transform);

        var variable = childGameObject.AddComponent(variableType) as AbstractMonoVariable;

        // 這裡可以進一步設定 VariableTag，如果有需要的話
        // variable._varTag = FindOrCreateVariableTag(tagName);

        return variable;
    }

    /// <summary>
    /// 根據 VariableTag 創建變數
    /// </summary>
    /// <typeparam name="TVariable">變數類型，必須繼承自 AbstractMonoVariable</typeparam>
    /// <param name="tag">要綁定的 VariableTag</param>
    /// <returns>創建的變數實例</returns>
    public TVariable CreateVariableWithTag<TVariable>(VariableTag tag)
        where TVariable : AbstractMonoVariable
    {
        if (tag == null)
        {
            Debug.LogError("VariableTag 不能為 null");
            return null;
        }

        var variable = gameObject.AddChildrenComponent<TVariable>($"[Var] {tag.name}");

        // 設定變數的 tag
        variable._varTag = tag;

        Debug.Log(
            $"已創建變數 {typeof(TVariable).Name} 並綁定到 VariableTag: {tag.name}",
            variable
        );

        return variable;
    }

    /// <summary>
    /// 根據 VariableTag 創建變數（非泛型版本）
    /// </summary>
    /// <param name="variableType">變數類型</param>
    /// <param name="tag">要綁定的 VariableTag</param>
    /// <returns>創建的變數實例</returns>
    public AbstractMonoVariable CreateVariableWithTag(Type variableType, VariableTag tag)
    {
        //只有Editor可以用對吧？有包了
        if (!typeof(AbstractMonoVariable).IsAssignableFrom(variableType))
        {
            Debug.LogError($"類型 {variableType} 不是 AbstractMonoVariable 的子類別");
            return null;
        }

        if (tag == null)
        {
            Debug.LogError("VariableTag 不能為 null");
            return null;
        }

        var childGameObject = new GameObject($"[Var] {tag.name}");
        childGameObject.transform.SetParent(transform);

        var variable = childGameObject.AddComp(variableType) as AbstractMonoVariable;

        // 設定變數的 tag
        variable._varTag = tag;

        Debug.Log($"已創建變數 {variableType.Name} 並綁定到 VariableTag: {tag.name}", variable);

        return variable;
    }
#endif

    #endregion

    protected override string DescriptionTag => "VarFolder";

    public void AfterSimulate(float deltaTime)
    {
        // this.Log($"VariableFolder AfterSimulate: {name}");
        CommitVariableValues();
    }
}
