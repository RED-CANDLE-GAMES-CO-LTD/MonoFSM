#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

/// <summary>
///     GameData config 表的 editor-only 疊層總覽：把「自己 _configs 的 override」和
///     「_baseConfig 疊層繼承來的值」合併成一張表，一列一個 tag，直接看得出哪些 key 存在、
///     目前生效值來自哪一層，並可一鍵 override / 還原。
///     runtime 完全不參與（整個檔案在 UNITY_EDITOR 內）。
/// </summary>
public partial class GameData
{
    #region Editor 端讀寫 helper（同 partial class 才碰得到 private 欄位）

    internal GameData EditorBaseConfig => _baseConfig;

    /// <summary>只查自己這一層的 _configs（不往 base 疊層）。</summary>
    internal bool EditorTryGetOwnConfig(VariableTag tag, out float value)
    {
        value = 0f;
        if (tag == null || _configs == null)
            return false;
        for (var i = 0; i < _configs.Length; i++)
        {
            var entry = _configs[i];
            if (entry?._tag != tag)
                continue;
            value = entry._value;
            return true;
        }

        return false;
    }

    internal bool EditorTryGetOwnObjConfig(VariableTag tag, out MonoObj value)
    {
        value = null;
        if (tag == null || _objConfigs == null)
            return false;
        for (var i = 0; i < _objConfigs.Length; i++)
        {
            var entry = _objConfigs[i];
            if (entry?._tag != tag)
                continue;
            value = entry._value;
            return true;
        }

        return false;
    }

    /// <summary>從 _baseConfig 那一層開始往上找第一個有這個 tag 的 GameData（不含自己）。</summary>
    internal GameData EditorFindBaseFloatSource(VariableTag tag, out float value)
    {
        value = 0f;
        var current = _baseConfig;
        for (var depth = 0; depth < MaxConfigDepth && current != null; depth++)
        {
            if (current.EditorTryGetOwnConfig(tag, out value))
                return current;
            current = current._baseConfig;
        }

        return null;
    }

    internal GameData EditorFindBaseObjSource(VariableTag tag, out MonoObj value)
    {
        value = null;
        var current = _baseConfig;
        for (var depth = 0; depth < MaxConfigDepth && current != null; depth++)
        {
            if (current.EditorTryGetOwnObjConfig(tag, out value))
                return current;
            current = current._baseConfig;
        }

        return null;
    }

    /// <summary>寫入 / 更新自己這一層的 float override。</summary>
    internal void EditorSetOwnConfig(VariableTag tag, float value)
    {
        if (tag == null)
            return;
        Undo.RecordObject(this, "Set GameData Config");
        _configs ??= System.Array.Empty<GameDataConfigEntry>();
        for (var i = 0; i < _configs.Length; i++)
            if (_configs[i]?._tag == tag)
            {
                _configs[i]._value = value;
                AfterConfigEdited();
                return;
            }

        var appended = new GameDataConfigEntry[_configs.Length + 1];
        System.Array.Copy(_configs, appended, _configs.Length);
        appended[_configs.Length] = new GameDataConfigEntry { _tag = tag, _value = value };
        _configs = appended;
        AfterConfigEdited();
    }

    internal void EditorRemoveOwnConfig(VariableTag tag)
    {
        if (tag == null || _configs == null)
            return;
        var index = -1;
        for (var i = 0; i < _configs.Length; i++)
            if (_configs[i]?._tag == tag)
            {
                index = i;
                break;
            }

        if (index < 0)
            return;

        Undo.RecordObject(this, "Remove GameData Config");
        var shrunk = new GameDataConfigEntry[_configs.Length - 1];
        System.Array.Copy(_configs, 0, shrunk, 0, index);
        System.Array.Copy(_configs, index + 1, shrunk, index, _configs.Length - index - 1);
        _configs = shrunk;
        AfterConfigEdited();
    }

    internal void EditorSetOwnObjConfig(VariableTag tag, MonoObj value)
    {
        if (tag == null)
            return;
        Undo.RecordObject(this, "Set GameData Obj Config");
        _objConfigs ??= System.Array.Empty<GameDataObjConfigEntry>();
        for (var i = 0; i < _objConfigs.Length; i++)
            if (_objConfigs[i]?._tag == tag)
            {
                _objConfigs[i]._value = value;
                AfterObjConfigEdited();
                return;
            }

        var appended = new GameDataObjConfigEntry[_objConfigs.Length + 1];
        System.Array.Copy(_objConfigs, appended, _objConfigs.Length);
        appended[_objConfigs.Length] = new GameDataObjConfigEntry { _tag = tag, _value = value };
        _objConfigs = appended;
        AfterObjConfigEdited();
    }

    internal void EditorRemoveOwnObjConfig(VariableTag tag)
    {
        if (tag == null || _objConfigs == null)
            return;
        var index = -1;
        for (var i = 0; i < _objConfigs.Length; i++)
            if (_objConfigs[i]?._tag == tag)
            {
                index = i;
                break;
            }

        if (index < 0)
            return;

        Undo.RecordObject(this, "Remove GameData Obj Config");
        var shrunk = new GameDataObjConfigEntry[_objConfigs.Length - 1];
        System.Array.Copy(_objConfigs, 0, shrunk, 0, index);
        System.Array.Copy(
            _objConfigs,
            index + 1,
            shrunk,
            index,
            _objConfigs.Length - index - 1
        );
        _objConfigs = shrunk;
        AfterObjConfigEdited();
    }

    private void AfterConfigEdited()
    {
        RebuildConfigDict();
        EditorUtility.SetDirty(this);
        //在 Odin draw（按鈕 / setter）當中直接改 list 結構會打斷迭代，延到下一幀重建
        EditorApplication.delayCall += RebuildConfigOverview;
    }

    private void AfterObjConfigEdited()
    {
        RebuildObjConfigDict();
        EditorUtility.SetDirty(this);
        EditorApplication.delayCall += RebuildConfigOverview;
    }

    #endregion

    #region 疊層總覽表

    private const string ConfigOverviewGroup = "Config 疊層總覽";

    [PropertyOrder(-20)]
    [FoldoutGroup(ConfigOverviewGroup, false)]
    [ShowInInspector]
    [DisplayAsString]
    [LabelText("疊層鏈")]
    private string ConfigChainText
    {
        get
        {
            var sb = new StringBuilder(name);
            var current = _baseConfig;
            for (var depth = 0; depth < MaxConfigDepth && current != null; depth++)
            {
                sb.Append(" → ").Append(current.name);
                current = current._baseConfig;
            }

            return sb.ToString();
        }
    }

    [PropertyOrder(-19)]
    [FoldoutGroup(ConfigOverviewGroup)]
    [ShowInInspector]
    [TableList(AlwaysExpanded = true, IsReadOnly = true, ShowPaging = false)]
    [LabelText("Float Config")]
    private List<GameDataFloatConfigRow> _floatConfigOverview;

    [PropertyOrder(-18)]
    [FoldoutGroup(ConfigOverviewGroup)]
    [ShowInInspector]
    [TableList(AlwaysExpanded = true, IsReadOnly = true, ShowPaging = false)]
    [LabelText("MonoObj Config")]
    private List<GameDataObjConfigRow> _objConfigOverview;

    [PropertyOrder(-17)]
    [FoldoutGroup(ConfigOverviewGroup)]
    [Button("重新掃描疊層", ButtonSizes.Small)]
    [OnInspectorInit]
    private void RebuildConfigOverview()
    {
        _floatConfigOverview ??= new List<GameDataFloatConfigRow>();
        _objConfigOverview ??= new List<GameDataObjConfigRow>();
        _floatConfigOverview.Clear();
        _objConfigOverview.Clear();

        var tags = new List<VariableTag>();
        CollectConfigTags(tags);
        for (var i = 0; i < tags.Count; i++)
            _floatConfigOverview.Add(new GameDataFloatConfigRow(this, tags[i]));

        tags.Clear();
        CollectObjConfigTags(tags);
        for (var i = 0; i < tags.Count; i++)
            _objConfigOverview.Add(new GameDataObjConfigRow(this, tags[i]));
    }

    #endregion
}

/// <summary>疊層總覽的一列（float 表）：值一律即時從 GameData 查，避免按鈕操作後顯示 stale。</summary>
public class GameDataFloatConfigRow
{
    private readonly GameData _owner;
    private readonly VariableTag _tag;

    public GameDataFloatConfigRow(GameData owner, VariableTag tag)
    {
        _owner = owner;
        _tag = tag;
    }

    [TableColumnWidth(160, false)]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Tag")]
    public VariableTag Tag => _tag;

    [TableColumnWidth(150, false)]
    [ShowInInspector]
    [DisplayAsString]
    [LabelText("來源")]
    public string Source
    {
        get
        {
            if (IsOwnOverride)
                return "★ 本層 override";
            var source = _owner.EditorFindBaseFloatSource(_tag, out _);
            return source != null ? "↑ " + source.name : "(查不到)";
        }
    }

    [TableColumnWidth(90, false)]
    [ShowInInspector]
    [DisplayAsString]
    [LabelText("Base 值")]
    public string BaseValueText =>
        _owner.EditorFindBaseFloatSource(_tag, out var baseValue) != null
            ? baseValue.ToString("0.###")
            : "-";

    //改值即 override：非本層的列直接輸入數字就會在本層建一筆 entry
    [TableColumnWidth(90, false)]
    [ShowInInspector]
    [LabelText("生效值")]
    public float Value
    {
        get => _owner.TryGetConfig(_tag, out var value) ? value : 0f;
        set => _owner.EditorSetOwnConfig(_tag, value);
    }

    public bool IsOwnOverride => _owner.EditorTryGetOwnConfig(_tag, out _);

    private string ToggleLabel => IsOwnOverride ? "還原" : "Override";

    [TableColumnWidth(95, false)]
    [Button("$ToggleLabel")]
    private void ToggleOverride()
    {
        if (IsOwnOverride)
        {
            //base 也沒有的話，還原＝這個 tag 直接消失，先問一聲
            if (_owner.EditorFindBaseFloatSource(_tag, out _) == null
                && !EditorUtility.DisplayDialog(
                    "移除 config",
                    $"{_tag.name} 只存在於本層，還原後這個 key 會直接消失。要繼續嗎？",
                    "移除",
                    "取消"))
                return;
            _owner.EditorRemoveOwnConfig(_tag);
        }
        else
        {
            //帶入 base 值當起始值，避免 override 完先變 0
            _owner.EditorFindBaseFloatSource(_tag, out var baseValue);
            _owner.EditorSetOwnConfig(_tag, baseValue);
        }
    }
}

/// <summary>疊層總覽的一列（MonoObj 表）。</summary>
public class GameDataObjConfigRow
{
    private readonly GameData _owner;
    private readonly VariableTag _tag;

    public GameDataObjConfigRow(GameData owner, VariableTag tag)
    {
        _owner = owner;
        _tag = tag;
    }

    [TableColumnWidth(160, false)]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Tag")]
    public VariableTag Tag => _tag;

    [TableColumnWidth(150, false)]
    [ShowInInspector]
    [DisplayAsString]
    [LabelText("來源")]
    public string Source
    {
        get
        {
            if (IsOwnOverride)
                return "★ 本層 override";
            var source = _owner.EditorFindBaseObjSource(_tag, out _);
            return source != null ? "↑ " + source.name : "(查不到)";
        }
    }

    [TableColumnWidth(140, false)]
    [ShowInInspector]
    [DisplayAsString]
    [LabelText("Base 值")]
    public string BaseValueText
    {
        get
        {
            if (_owner.EditorFindBaseObjSource(_tag, out var baseValue) == null)
                return "-";
            return baseValue != null ? baseValue.name : "null";
        }
    }

    [TableColumnWidth(180, false)]
    [ShowInInspector]
    [LabelText("生效值")]
    public MonoObj Value
    {
        get => _owner.TryGetObjConfig(_tag, out var value) ? value : null;
        set => _owner.EditorSetOwnObjConfig(_tag, value);
    }

    public bool IsOwnOverride => _owner.EditorTryGetOwnObjConfig(_tag, out _);

    private string ToggleLabel => IsOwnOverride ? "還原" : "Override";

    [TableColumnWidth(95, false)]
    [Button("$ToggleLabel")]
    private void ToggleOverride()
    {
        if (IsOwnOverride)
        {
            if (_owner.EditorFindBaseObjSource(_tag, out _) == null
                && !EditorUtility.DisplayDialog(
                    "移除 obj config",
                    $"{_tag.name} 只存在於本層，還原後這個 key 會直接消失。要繼續嗎？",
                    "移除",
                    "取消"))
                return;
            _owner.EditorRemoveOwnObjConfig(_tag);
        }
        else
        {
            _owner.EditorFindBaseObjSource(_tag, out var baseValue);
            _owner.EditorSetOwnObjConfig(_tag, baseValue);
        }
    }
}
#endif
