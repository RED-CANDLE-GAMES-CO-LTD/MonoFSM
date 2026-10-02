using System;
using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using UnityEngine;

public abstract class AbstractFolder : MonoBehaviour
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

//FIXME: 這個才該叫做blackboard?，這個是用來放變數的?

/// <summary>
/// entity 的變數字典（GetVar 的來源）：SceneAwake 時把子樹所有 var（含 inactive）依 _varTag 收進來，AfterSimulate 統一 CommitValue。
/// 同 tag 撞名時 active 那顆贏、不印 log；兩顆都 inactive 印 Warning；兩顆都 active 才 LogError（先到的贏）。
/// </summary>
public class VariableFolder : MonoDictFolder<VariableTag, AbstractMonoVariable>, IAfterSimulate
{
    private Dictionary<string, AbstractMonoVariable> _nameMap = new();

    private bool _initialized;
    //FIXME: external dict?
    protected override bool IsStringDictEnable => true;

    protected override bool IsAddValid(AbstractMonoVariable value)
    {
        if (value.HasParentVarEntity) //這段擋掉對嗎？因為我想要宣告Train.HasPower
            return false;

        //現在很深喔，所有下面的變數包含getter都撈出來（含 inactive，見 MonoDict._collections）
        var tag = value._varTag;
        if (tag == null || !_dict.TryGetValue(tag, out var existing) || existing == null)
            return true;

        //同 tag 撞名：inactive（「關掉 = 設 inactive」像註解一樣保留的舊節點）讓給 active 那顆。
        //不能整批跳過 inactive：dict 只在 SceneAwake 建一次，之後才被 SetActive 打開的 var 會永遠查不到。
        //這段只在撞名時走，正常路徑不配置記憶體；log 字串只在真的撞到時才組。
        //active 對 inactive 是正常用法（舊節點關掉留著當註解），不印 log。
        var newActive = IsActiveUnderFolder(value);
        var oldActive = IsActiveUnderFolder(existing);
        if (newActive && !oldActive)
        {
            ReplaceExisting(tag, existing);
            return true;
        }

        if (oldActive && !newActive)
            return false;

        if (!newActive) //兩顆都 inactive：都是關掉的節點，先到的留著，提醒一下就好
        {
            Debug.LogWarning(
                $"[VariableFolder] '{name}' tag '{tag.name}' 兩顆 inactive var 撞名，保留 {existing.transform.GetPath()}，忽略 {value.transform.GetPath()}",
                value);
            return false;
        }

        //兩顆都 active 才是真的設計問題（GetVar 會撈到不確定的變數），維持 LogError、先到的贏
        Debug.LogError(
            $"[VariableFolder] Variable with tag '{tag.name}' already exists in folder '{name}'. Please ensure each variable has a unique tag. kept:{existing.transform.GetPath()} dropped:{value.transform.GetPath()}",
            value);
        return false;
    }

    /// <summary>
    /// var 相對於這個 folder 是否 active：只看 var 到 folder 之間每一層的 activeSelf。
    /// 不用 activeInHierarchy，因為 SceneAwake 時整個 entity 可能還是 inactive（pool / 尚未啟用），那時每顆都會被當成 inactive。
    /// </summary>
    private bool IsActiveUnderFolder(AbstractMonoVariable v)
    {
        var root = transform;
        var t = v.transform;
        while (t != null && t != root)
        {
            if (!t.gameObject.activeSelf)
                return false;
            t = t.parent;
        }

        return true;
    }

    /// <summary>
    /// 撞名時把先收進來的那顆拿掉。MonoDict.Remove 不會清 _stringDict，
    /// 不手動清的話 Add 的 TryAdd 會失敗，GetVariable(string) 還是拿到舊的那顆。
    /// </summary>
    private void ReplaceExisting(VariableTag tag, AbstractMonoVariable existing)
    {
        Remove(tag);
        if (!IsStringDictEnable)
            return;
        var stringKey = tag.ToString();
        if (_stringDict.TryGetValue(stringKey, out var mapped) && mapped == existing)
            _stringDict.Remove(stringKey);
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
