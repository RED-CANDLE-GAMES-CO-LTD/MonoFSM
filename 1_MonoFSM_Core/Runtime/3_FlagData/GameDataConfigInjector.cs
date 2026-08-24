using System;
using System.Collections.Generic;
using MonoFSM.Core.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Variable
{
    /// <summary>
    ///     掛在 entity 的 VariableFolder 節點上：IResetStart 時把綁定 GameData 的 config 表
    ///     依 VariableTag 一次性注入 folder 底下的 VarFloat。
    ///     語意＝config 是「初始值表」，注入後 VarFloat 就是可寫的執行期狀態，
    ///     config 沒填的 tag 保留 prefab local value（per-prefab override 天然成立）。
    ///     優先序：prefab 顯式覆蓋（_skipTags）&gt; GameData config &gt; prefab 預設值。
    ///     用 IResetStart 而不是 ISceneStart / IResetStateRestore，後兩者會被 Field.Init 蓋掉。
    ///     TODO: late-join client 可能在收到 host 當前值後被本地注入蓋回初始值；
    ///     目前開場兩端 config 相同所以無害，遇到問題時再加 State Authority gate。
    /// </summary>
    public class GameDataConfigInjector : MonoBehaviour, IResetStart
    {
        [AutoParent] [SerializeField] private VariableFolder _folder;

        [SerializeField] private VarGameDataWrapper _bindData; //指到 entity 下那顆 VarGameData

        //這些 tag 不注入，讓 prefab 上的 local value 贏（variant 微調用）
        [SerializeField] private VariableTag[] _skipTags = Array.Empty<VariableTag>();

        public void ResetStart()
        {
            Inject();
        }

        [Button] //GameData 執行期換掉（換 item / 換車廂類型）時由 FSM Action 呼叫
        public void Inject()
        {
            if (_folder == null)
            {
                Debug.Log("[ConfigInject] 找不到 VariableFolder，跳過注入", this);
                return;
            }

            if (_bindData == null)
            {
                Debug.Log("[ConfigInject] _bindData 沒設，跳過注入", this);
                return;
            }

            var data = _bindData.Value;
            if (data == null)
            {
                // Debug.Log("[ConfigInject] bindData 為 null，用 prefab local value", this);
                return;
            }

            var comps = _folder.AllValues;
            if (comps == null)
                return;

            for (var i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                if (comp == null)
                    continue;
                var tag = comp._varTag;
                if (tag == null || IsSkipped(tag))
                    continue;

                if (comp is VarFloat varFloat && comp.HasProxySource == false)
                {
                    if (data.TryGetConfig(tag, out var value))
                        varFloat.SetValue(value, this);
                }
                else if (comp is VarMonoObj varMonoObj)
                {
                    //VarMonoObj 走 GenericUnityObjectVariable：SetValue 對非 _isRuntimeOnly 會 LogError 擋掉，
                    //config = 初始值表的語意要用 SetOverrideDefaultValue（同時寫 _tempValue 與 _defaultValue）
                    if (data.TryGetObjConfig(tag, out var objValue))
                        varMonoObj.SetOverrideDefaultValue(objValue, this);
                }
            }
        }

        //線性比對，數量很少，不用 HashSet 避免 GC
        private bool IsSkipped(VariableTag tag)
        {
            if (_skipTags == null)
                return false;
            for (var i = 0; i < _skipTags.Length; i++)
                if (ReferenceEquals(_skipTags[i], tag))
                    return true;
            return false;
        }

        //editor 用的 folder fallback：_folder 是 [AutoParent]，還沒 auto reference 過時自己找
        private VariableFolder ResolveFolder()
        {
            if (_folder != null)
                return _folder;
            return GetComponentInParent<VariableFolder>(true);
        }

#if UNITY_EDITOR
        /// <summary>
        ///     editor 工具：讀綁定 GameData 的 config 表（含 _baseConfig 疊層），
        ///     VariableFolder 底下缺哪個 tag 的 VarFloat 就建一顆，並把 FlagField 預設值設成 config 值。
        ///     已存在同 tag 的不動（prefab 顯式覆蓋優先）。
        /// </summary>
        [PropertyOrder(100)]
        [Button("依 GameData config 補齊 VarFloat", ButtonSizes.Medium)]
        private void EditorFillMissingVarFloats()
        {
            var folder = ResolveFolder();
            if (folder == null)
            {
                Debug.LogError("[ConfigInject] 找不到 VariableFolder（要掛在 VariableFolder 或其子節點下）", this);
                return;
            }

            var data = _bindData?.Value;
            if (data == null)
            {
                Debug.LogError("[ConfigInject] _bindData 沒指到 GameData（editor 讀的是 VarGameData 的 _defaultValue）", this);
                return;
            }

            var tags = new List<VariableTag>();
            data.CollectConfigTags(tags);
            data.CollectObjConfigTags(tags); //MonoObj 表的 tag 接在後面，一起補齊
            if (tags.Count == 0)
            {
                Debug.LogWarning($"[ConfigInject] {data.name} 沒有任何 config entry", data);
                return;
            }

            //既有的 tag（整個 folder 子樹，含 disabled）
            var existing = new HashSet<VariableTag>();
            foreach (var v in folder.GetComponentsInChildren<AbstractMonoVariable>(true))
                if (v._varTag != null)
                    existing.Add(v._varTag);

            var created = 0;
            foreach (var tag in tags)
            {
                if (tag == null || existing.Contains(tag))
                    continue;

                //在 MonoObj 表裡的 tag 建 VarMonoObj（用 _objConfigs 的 prefab 當 _defaultValue）
                if (data.HasObjConfig(tag))
                {
                    if (!data.TryGetObjConfig(tag, out var objValue))
                        continue;

                    var varMonoObj =
                        folder.gameObject.AddChildrenComponent(
                            typeof(VarMonoObj),
                            $"[Var] {tag.name}"
                        ) as VarMonoObj;
                    if (varMonoObj == null)
                    {
                        Debug.LogError($"[ConfigInject] 建立 VarMonoObj 失敗: {tag.name}", this);
                        continue;
                    }

                    varMonoObj._varTag = tag;
                    //_defaultValue 是 protected，editor 端走 SerializedObject 寫入
                    var so = new SerializedObject(varMonoObj);
                    var defaultProp = so.FindProperty("_defaultValue");
                    if (defaultProp != null)
                    {
                        defaultProp.objectReferenceValue = objValue;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    else
                    {
                        Debug.LogError(
                            $"[ConfigInject] VarMonoObj 找不到 _defaultValue 欄位: {tag.name}",
                            varMonoObj
                        );
                    }

                    existing.Add(tag);
                    created++;
                    EditorUtility.SetDirty(varMonoObj);
                    EditorUtility.SetDirty(varMonoObj.gameObject);
                    continue;
                }

                //只處理 float 型 tag（ValueType 沒設就當成 float，config 表本來就只有 float）
                var valueType = tag.ValueType;
                if (valueType != null && valueType != typeof(float))
                {
                    Debug.LogWarning(
                        $"[ConfigInject] tag {tag.name} 的 ValueType 是 {valueType.Name}，不是 float，跳過",
                        data
                    );
                    continue;
                }

                if (!data.TryGetConfig(tag, out var configValue))
                    continue;

                var varFloat =
                    folder.gameObject.AddChildrenComponent(typeof(VarFloat), $"[Var] {tag.name}")
                        as VarFloat;
                if (varFloat == null)
                {
                    Debug.LogError($"[ConfigInject] 建立 VarFloat 失敗: {tag.name}", this);
                    continue;
                }

                varFloat._varTag = tag;
                varFloat._localField.ProductionValue = configValue;
                varFloat._localField.DevValue = configValue;
                existing.Add(tag);
                created++;
                EditorUtility.SetDirty(varFloat);
                EditorUtility.SetDirty(varFloat.gameObject);
            }

            if (created > 0)
            {
                EditorUtility.SetDirty(folder);
                Debug.Log($"[ConfigInject] 依 {data.name} 補齊了 {created} 顆 Var", this);
            }
            else
            {
                Debug.Log($"[ConfigInject] {data.name} 的 config tag 都已存在，沒有補齊任何 Var", this);
            }
        }

        #region 給 Var inspector 用的 editor API

        public GameData EditorBoundGameData => _bindData != null ? _bindData.Value : null;

        public bool IsTagSkipped(VariableTag tag)
        {
            return IsSkipped(tag);
        }

        public void EditorAddSkipTag(VariableTag tag)
        {
            if (tag == null || IsSkipped(tag))
                return;
            Undo.RecordObject(this, "Add Config Skip Tag");
            _skipTags ??= Array.Empty<VariableTag>();
            var appended = new VariableTag[_skipTags.Length + 1];
            Array.Copy(_skipTags, appended, _skipTags.Length);
            appended[_skipTags.Length] = tag;
            _skipTags = appended;
            EditorUtility.SetDirty(this);
            EditorApplication.delayCall += RebuildInjectPreview;
        }

        public void EditorRemoveSkipTag(VariableTag tag)
        {
            if (tag == null || _skipTags == null)
                return;
            var index = Array.IndexOf(_skipTags, tag);
            if (index < 0)
                return;
            Undo.RecordObject(this, "Remove Config Skip Tag");
            var shrunk = new VariableTag[_skipTags.Length - 1];
            Array.Copy(_skipTags, 0, shrunk, 0, index);
            Array.Copy(_skipTags, index + 1, shrunk, index, _skipTags.Length - index - 1);
            _skipTags = shrunk;
            EditorUtility.SetDirty(this);
            EditorApplication.delayCall += RebuildInjectPreview;
        }

        #endregion

        #region 注入對照表

        [PropertyOrder(95)]
        [LabelText("列出不受 config 管的 Var")]
        [ShowInInspector]
        [OnValueChanged(nameof(RebuildInjectPreview))]
        private bool _showLocalOnlyVars;

        [PropertyOrder(96)]
        [ShowInInspector]
        [TableList(AlwaysExpanded = true, IsReadOnly = true, ShowPaging = false)]
        [LabelText("注入對照表")]
        private List<ConfigInjectPreviewRow> _injectPreview;

        [PropertyOrder(97)]
        [Button("重新掃描注入對照", ButtonSizes.Small)]
        [OnInspectorInit]
        private void RebuildInjectPreview()
        {
            _injectPreview ??= new List<ConfigInjectPreviewRow>();
            _injectPreview.Clear();

            var folder = ResolveFolder();
            var data = EditorBoundGameData;
            if (folder == null || data == null)
                return;

            var vars = folder.GetComponentsInChildren<AbstractMonoVariable>(true);

            var tags = new List<VariableTag>();
            data.CollectConfigTags(tags);
            var floatTagCount = tags.Count; //前段是 float 表的 tag，後段是 obj 表
            data.CollectObjConfigTags(tags);

            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (tag == null)
                    continue;
                //obj 表段落裡若該 tag 也在 float 表出現過就跳過，避免重複列
                if (i >= floatTagCount && tags.IndexOf(tag) < floatTagCount)
                    continue;
                _injectPreview.Add(new ConfigInjectPreviewRow(this, tag, FindVar(vars, tag), true));
            }

            if (!_showLocalOnlyVars)
                return;

            for (var i = 0; i < vars.Length; i++)
            {
                var v = vars[i];
                if (v == null || v._varTag == null)
                    continue;
                if (data.HasConfig(v._varTag) || data.HasObjConfig(v._varTag))
                    continue;
                _injectPreview.Add(new ConfigInjectPreviewRow(this, v._varTag, v, false));
            }
        }

        private static AbstractMonoVariable FindVar(AbstractMonoVariable[] vars, VariableTag tag)
        {
            for (var i = 0; i < vars.Length; i++)
                if (vars[i] != null && vars[i]._varTag == tag)
                    return vars[i];
            return null;
        }

        /// <summary>
        ///     一列＝一個 tag：看得出 config 表與 folder 底下的 Var 怎麼對上，
        ///     以及注入後這顆 Var 的值會變成什麼。
        /// </summary>
        public class ConfigInjectPreviewRow
        {
            private readonly GameDataConfigInjector _injector;
            private readonly VariableTag _tag;
            private readonly AbstractMonoVariable _var;
            private readonly bool _hasConfig;

            public ConfigInjectPreviewRow(
                GameDataConfigInjector injector,
                VariableTag tag,
                AbstractMonoVariable var,
                bool hasConfig
            )
            {
                _injector = injector;
                _tag = tag;
                _var = var;
                _hasConfig = hasConfig;
            }

            [TableColumnWidth(150, false)]
            [ShowInInspector]
            [ReadOnly]
            [LabelText("Tag")]
            public VariableTag Tag => _tag;

            [TableColumnWidth(140, false)]
            [ShowInInspector]
            [DisplayAsString]
            [LabelText("狀態")]
            public string Status
            {
                get
                {
                    if (!_hasConfig)
                        return "只有本地值";
                    if (_var == null)
                        return "缺 Var（可按補齊）";
                    return _injector.IsTagSkipped(_tag) ? "本地值優先 (skip)" : "會被注入";
                }
            }

            [TableColumnWidth(170, false)]
            [ShowInInspector]
            [ReadOnly]
            [LabelText("Var 節點")]
            public AbstractMonoVariable Var => _var;

            [TableColumnWidth(90, false)]
            [ShowInInspector]
            [DisplayAsString]
            [LabelText("本地值")]
            public string LocalValueText
            {
                get
                {
                    if (_var is VarFloat varFloat)
                        return varFloat._localField != null
                            ? varFloat._localField.ProductionValue.ToString("0.###")
                            : "-";
                    return "-";
                }
            }

            [TableColumnWidth(90, false)]
            [ShowInInspector]
            [DisplayAsString]
            [LabelText("Config 值")]
            public string ConfigValueText
            {
                get
                {
                    var data = _injector.EditorBoundGameData;
                    if (data == null || !_hasConfig)
                        return "-";
                    if (data.TryGetConfig(_tag, out var floatValue))
                        return floatValue.ToString("0.###");
                    if (data.TryGetObjConfig(_tag, out var objValue))
                        return objValue != null ? objValue.name : "null";
                    return "-";
                }
            }

            private bool CanToggleSkip => _hasConfig && _var != null;

            private string SkipToggleLabel => _injector.IsTagSkipped(_tag) ? "取消 skip" : "設為 skip";

            [TableColumnWidth(95, false)]
            [ShowIf(nameof(CanToggleSkip))]
            [Button("$SkipToggleLabel")]
            private void ToggleSkip()
            {
                if (_injector.IsTagSkipped(_tag))
                    _injector.EditorRemoveSkipTag(_tag);
                else
                    _injector.EditorAddSkipTag(_tag);
            }
        }

        #endregion
#endif
    }
}
