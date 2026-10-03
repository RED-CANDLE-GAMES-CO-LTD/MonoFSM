using System;
using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AbstractFolder : MonoBehaviour
{
    public string IconName => "Folder Icon";
    public bool IsDrawingIcon => true;
}

//FIXME: 這個才該叫做blackboard?，這個是用來放變數的?

/// <summary>
/// entity 的變數字典（GetVar 的來源）：SceneAwake 時把子樹所有 var（含 inactive）依 _varTag 收進來；AfterSimulate 只 CommitValue 這個 tick 值有變（有登記 pending）的 var。
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

    //--- commit：只 commit 值有變的 var ---
    //舊做法每 tick 對 _collections（子樹全部 var）逐顆 CommitValue，~580 個 folder 合計 4ms/frame。
    //CommitValue 只做 _lastValue = CurrentValue（ValueCommited 目前沒有任何 override），值沒變時是 no-op，
    //所以改成 var 值變時自己登記（AbstractMonoVariable.MarkCommitPending），這裡只 commit 有登記的。
    //設計理由、哪些寫入點有登記、為什麼目前沒有輪詢型 var：見同資料夾 Progress.md。
    [NonSerialized] private AbstractMonoVariable[] _commitPending = Array.Empty<AbstractMonoVariable>();
    [NonSerialized] private int _commitPendingCount;
    [NonSerialized] private AbstractMonoVariable[] _commitPolled = Array.Empty<AbstractMonoVariable>();
    [NonSerialized] private int _commitPolledCount;
    [NonSerialized] private bool _isCommitBound;

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] [FoldoutGroup("Commit Debug")]
    private int CommitPendingCount => _commitPendingCount;

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] [FoldoutGroup("Commit Debug")]
    private int CommitPolledCount => _commitPolledCount;

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] [FoldoutGroup("Commit Debug")]
    [NonSerialized] private int _commitOwnedCount;

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] [FoldoutGroup("Commit Debug")]
    [LabelText("上個 tick commit 數")]
    [NonSerialized] private int _lastTickCommitCount;

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] [FoldoutGroup("Commit Debug")]
    [LabelText("上個 tick 略過（proxy / 已銷毀）")]
    [NonSerialized] private int _lastTickCommitSkippedCount;

    [ShowInInspector] [Sirenix.OdinInspector.ReadOnly] [FoldoutGroup("Commit Debug")]
    [LabelText("被更近的 folder 認領")]
    [NonSerialized] private int _commitOwnedByNestedCount;

    public override void EnterSceneAwake()
    {
        base.EnterSceneAwake(); //Refresh：重建 _collections / dict
        BindCommitVariables();
    }

    /// <summary>
    /// 把 _collections 裡的 var 綁到這個 folder（巢狀時歸屬最近的 folder，不會被兩個 folder 重複 commit），
    /// 並把每顆登記一次，讓第一個 tick 補齊綁定前的狀態。
    /// </summary>
    private void BindCommitVariables()
    {
        _isCommitBound = true;
        var collections = _collections;
        if (collections == null)
        {
            Debug.LogWarning($"[VariableFolder] '{name}' 綁 commit 時 _collections 是 null，這個 folder 不會 commit 任何 var", this);
            return;
        }

        //先把容量配夠，綁定過程中的 EnqueueCommit 就不會擴容
        if (_commitPending.Length < collections.Length)
        {
            var grown = new AbstractMonoVariable[collections.Length];
            Array.Copy(_commitPending, grown, _commitPendingCount);
            _commitPending = grown;
        }

        if (_commitPolled.Length < collections.Length)
            _commitPolled = new AbstractMonoVariable[collections.Length];
        else
            Array.Clear(_commitPolled, 0, _commitPolledCount);
        _commitPolledCount = 0;
        _commitOwnedCount = 0;
        _commitOwnedByNestedCount = 0;

        foreach (var variable in collections)
        {
            if (variable == null)
                continue;
            if (!variable.BindCommitFolder(this))
            {
                _commitOwnedByNestedCount++;
                continue;
            }

            _commitOwnedCount++;
            if (variable.IsCommitPolledEveryTick)
                _commitPolled[_commitPolledCount++] = variable;
        }
    }

    /// <summary>由 AbstractMonoVariable.MarkCommitPending 呼叫；同一顆在 commit 前只會進來一次（var 上的 flag 擋）。</summary>
    internal void EnqueueCommit(AbstractMonoVariable variable)
    {
        if (_commitPendingCount == _commitPending.Length)
        {
            //正常不會走到（容量 = _collections 長度，每顆最多一筆）；runtime 才掛進來的 var 之類的例外才擴容
            Array.Resize(ref _commitPending, Math.Max(8, _commitPending.Length * 2));
        }

        _commitPending[_commitPendingCount++] = variable;
    }

    public void CommitVariableValues()
    {
        if (!_isCommitBound) //沒走到 EnterSceneAwake 的保險
            BindCommitVariables();

        var committed = 0;
        var skipped = 0;

        //只處理進來時已登記的；commit 過程中才登記的留到下一個 tick（跟舊做法「一個 tick commit 一次」一致）
        var count = _commitPendingCount;
        for (var i = 0; i < count; i++)
        {
            var variable = _commitPending[i];
            _commitPending[i] = null;
            variable._isCommitPending = false;
            if (CommitOne(variable))
                committed++;
            else
                skipped++;
        }

        var rest = _commitPendingCount - count;
        if (rest > 0)
        {
            Array.Copy(_commitPending, count, _commitPending, 0, rest);
            Array.Clear(_commitPending, rest, count);
        }

        _commitPendingCount = rest;

        for (var i = 0; i < _commitPolledCount; i++)
        {
            var variable = _commitPolled[i];
            //被更深的 folder 搶走（runtime 才出現巢狀）就交給它
            if (!ReferenceEquals(variable.CommitFolder, this))
                continue;
            if (CommitOne(variable))
                committed++;
            else
                skipped++;
        }

        _lastTickCommitCount = committed;
        _lastTickCommitSkippedCount = skipped;
    }

    private static bool CommitOne(AbstractMonoVariable variable)
    {
        if (variable == null) //已銷毀
            return false;
        if (variable.HasProxySource)
            return false;
        if (variable is ISettable settableVariable)
        {
            settableVariable.CommitValue();
            return true;
        }

        return false;
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
