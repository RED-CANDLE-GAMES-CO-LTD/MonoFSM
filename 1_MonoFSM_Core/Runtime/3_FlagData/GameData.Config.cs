using System;
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
///     GameData 上的 config 數值表：用 VariableTag 當 key，讓 prefab 上的 VarFloat 可以透過
///     GameDataConfigValueSource 從這裡取值，而不是各自在 prefab 上填 local value。
///     prefab 端不換型別、不改結構，所有既有消費端零 migrate。
/// </summary>
[Serializable]
public class GameDataConfigEntry
{
    [SOConfig("VariableType")] [SerializeField]
    internal VariableTag _tag;

    [SerializeField] internal float _value;

    public override string ToString()
    {
        return _tag != null ? $"{_tag.name} = {_value}" : $"(no tag) = {_value}";
    }
}

/// <summary>
///     跟 GameDataConfigEntry 對稱的 MonoObj 版本：float 走值、Object 走 Unity 引用，
///     分開兩張表最單純且無 boxing。
/// </summary>
[Serializable]
public class GameDataObjConfigEntry
{
    [SOConfig("VariableType")] [SerializeField]
    internal VariableTag _tag;

    [PrefabFilter] [SerializeField] internal MonoObj _value;

    public override string ToString()
    {
        return _tag != null
            ? $"{_tag.name} = {(_value != null ? _value.name : "null")}"
            : $"(no tag) = {_value}";
    }
}

public partial class GameData
{
    [OnCollectionChanged(nameof(RebuildConfigDict))] [TableList] [SerializeField]
    private GameDataConfigEntry[] _configs = Array.Empty<GameDataConfigEntry>();

    [OnCollectionChanged(nameof(RebuildObjConfigDict))] [TableList] [SerializeField]
    private GameDataObjConfigEntry[] _objConfigs = Array.Empty<GameDataObjConfigEntry>();

    //疊層用：variant 的 GameData 只存 delta，自己的 _configs 查不到時 fallback 到 base 繼續查。
    //可以留 null（就是沒有 base）。防循環：查詢深度上限 MaxConfigDepth。
    [SerializeField] private GameData _baseConfig;

    //給同 partial class 的其他檔案（GameData.cs 的 _bindPrefab 疊層）用
    private GameData BaseConfig => _baseConfig;

    //防循環引用（A.base = B, B.base = A）：超過就報錯停下，不用 HashSet 避免 GC
    private const int MaxConfigDepth = 8;

    //key = tag.GetInstanceID()：避開 UnityEngine.Object 覆寫 == 的 overhead，也不會 boxing
    private readonly Dictionary<int, float> _configDict = new();

    [ShowInDebugMode] public int ConfigCount => _configDict.Count;

    [ShowInDebugMode]
    private bool IsConfigRebuildNeeded =>
        _configs != null && _configs.Length != _configDict.Count;

    private void RebuildConfigCheck()
    {
        if (IsConfigRebuildNeeded)
            RebuildConfigDict();
    }

    private void RebuildConfigDict()
    {
        if (_configs == null)
            return;
        _configDict.Clear();
        for (var i = 0; i < _configs.Length; i++)
        {
            var entry = _configs[i];
            if (entry == null)
            {
                Debug.LogError($"[GameDataConfig] config entry {i} is null in {name}", this);
                continue;
            }

            if (entry._tag == null)
            {
                Debug.LogError($"[GameDataConfig] config entry {i} 沒設 tag in {name}", this);
                continue;
            }

            if (!_configDict.TryAdd(entry._tag.GetInstanceID(), entry._value))
                Debug.LogError(
                    $"[GameDataConfig] Duplicate config tag {entry._tag.name} in {name}",
                    this
                );
        }
    }

    public bool TryGetConfig(VariableTag tag, out float value)
    {
        return TryGetConfigInternal(tag, out value, 0);
    }

    /// <summary>
    ///     蒐集自己 + 疊層 base 的所有 config tag（去重）。給 editor 工具用（補齊按鈕 / schema validate），
    ///     runtime 熱路徑不要呼叫（會配置 List）。
    /// </summary>
    public void CollectConfigTags(List<VariableTag> buffer)
    {
        if (buffer == null)
            return;
        CollectConfigTagsInternal(buffer, 0);
    }

    private void CollectConfigTagsInternal(List<VariableTag> buffer, int depth)
    {
        if (depth >= MaxConfigDepth)
        {
            Debug.LogError(
                $"[GameDataConfig] _baseConfig 疊層超過 {MaxConfigDepth} 層，可能有循環引用: {name}",
                this
            );
            return;
        }

        if (_configs != null)
            for (var i = 0; i < _configs.Length; i++)
            {
                var entry = _configs[i];
                if (entry?._tag == null)
                    continue;
                if (!buffer.Contains(entry._tag))
                    buffer.Add(entry._tag);
            }

        if (_baseConfig != null)
            _baseConfig.CollectConfigTagsInternal(buffer, depth + 1);
    }

    //自己查不到就往 base 疊層查（base 的 rebuild 由 base 自己的 TryGetConfig 觸發）
    private bool TryGetConfigInternal(VariableTag tag, out float value, int depth)
    {
        value = 0f;
        if (tag == null)
            return false;
        if (depth >= MaxConfigDepth)
        {
            Debug.LogError(
                $"[GameDataConfig] _baseConfig 疊層超過 {MaxConfigDepth} 層，可能有循環引用: {name}",
                this
            );
            return false;
        }

        RebuildConfigCheck();
        if (_configDict.TryGetValue(tag.GetInstanceID(), out value))
            return true;
        if (_baseConfig == null)
            return false;
        return _baseConfig.TryGetConfigInternal(tag, out value, depth + 1);
    }

    public bool HasConfig(VariableTag tag)
    {
        return HasConfigInternal(tag, 0);
    }

    private bool HasConfigInternal(VariableTag tag, int depth)
    {
        if (tag == null)
            return false;
        if (depth >= MaxConfigDepth)
        {
            Debug.LogError(
                $"[GameDataConfig] _baseConfig 疊層超過 {MaxConfigDepth} 層，可能有循環引用: {name}",
                this
            );
            return false;
        }

        RebuildConfigCheck();
        if (_configDict.ContainsKey(tag.GetInstanceID()))
            return true;
        if (_baseConfig == null)
            return false;
        return _baseConfig.HasConfigInternal(tag, depth + 1);
    }

    #region MonoObj config 表（跟上面 float 表完全對稱）

    //key = tag.GetInstanceID()
    private readonly Dictionary<int, MonoObj> _objConfigDict = new();

    [ShowInDebugMode] public int ObjConfigCount => _objConfigDict.Count;

    [ShowInDebugMode]
    private bool IsObjConfigRebuildNeeded =>
        _objConfigs != null && _objConfigs.Length != _objConfigDict.Count;

    private void RebuildObjConfigCheck()
    {
        if (IsObjConfigRebuildNeeded)
            RebuildObjConfigDict();
    }

    private void RebuildObjConfigDict()
    {
        if (_objConfigs == null)
            return;
        _objConfigDict.Clear();
        for (var i = 0; i < _objConfigs.Length; i++)
        {
            var entry = _objConfigs[i];
            if (entry == null)
            {
                Debug.LogError($"[GameDataObjConfig] obj config entry {i} is null in {name}", this);
                continue;
            }

            if (entry._tag == null)
            {
                Debug.LogError($"[GameDataObjConfig] obj config entry {i} 沒設 tag in {name}", this);
                continue;
            }

            if (!_objConfigDict.TryAdd(entry._tag.GetInstanceID(), entry._value))
                Debug.LogError(
                    $"[GameDataObjConfig] Duplicate obj config tag {entry._tag.name} in {name}",
                    this
                );
        }
    }

    public bool TryGetObjConfig(VariableTag tag, out MonoObj value)
    {
        return TryGetObjConfigInternal(tag, out value, 0);
    }

    private bool TryGetObjConfigInternal(VariableTag tag, out MonoObj value, int depth)
    {
        value = null;
        if (tag == null)
            return false;
        if (depth >= MaxConfigDepth)
        {
            Debug.LogError(
                $"[GameDataObjConfig] _baseConfig 疊層超過 {MaxConfigDepth} 層，可能有循環引用: {name}",
                this
            );
            return false;
        }

        RebuildObjConfigCheck();
        if (_objConfigDict.TryGetValue(tag.GetInstanceID(), out value))
            return true;
        if (_baseConfig == null)
            return false;
        return _baseConfig.TryGetObjConfigInternal(tag, out value, depth + 1);
    }

    public bool HasObjConfig(VariableTag tag)
    {
        return HasObjConfigInternal(tag, 0);
    }

    private bool HasObjConfigInternal(VariableTag tag, int depth)
    {
        if (tag == null)
            return false;
        if (depth >= MaxConfigDepth)
        {
            Debug.LogError(
                $"[GameDataObjConfig] _baseConfig 疊層超過 {MaxConfigDepth} 層，可能有循環引用: {name}",
                this
            );
            return false;
        }

        RebuildObjConfigCheck();
        if (_objConfigDict.ContainsKey(tag.GetInstanceID()))
            return true;
        if (_baseConfig == null)
            return false;
        return _baseConfig.HasObjConfigInternal(tag, depth + 1);
    }

    /// <summary>
    ///     蒐集自己 + 疊層 base 的所有 obj config tag（去重）。給 editor 工具用，runtime 熱路徑不要呼叫。
    /// </summary>
    public void CollectObjConfigTags(List<VariableTag> buffer)
    {
        if (buffer == null)
            return;
        CollectObjConfigTagsInternal(buffer, 0);
    }

    private void CollectObjConfigTagsInternal(List<VariableTag> buffer, int depth)
    {
        if (depth >= MaxConfigDepth)
        {
            Debug.LogError(
                $"[GameDataObjConfig] _baseConfig 疊層超過 {MaxConfigDepth} 層，可能有循環引用: {name}",
                this
            );
            return;
        }

        if (_objConfigs != null)
            for (var i = 0; i < _objConfigs.Length; i++)
            {
                var entry = _objConfigs[i];
                if (entry?._tag == null)
                    continue;
                if (!buffer.Contains(entry._tag))
                    buffer.Add(entry._tag);
            }

        if (_baseConfig != null)
            _baseConfig.CollectObjConfigTagsInternal(buffer, depth + 1);
    }

    #endregion
}
