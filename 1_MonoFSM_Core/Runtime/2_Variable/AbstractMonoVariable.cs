using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
#if UNITY_EDITOR
using _0_MonoDebug.Gizmo;
#endif
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.CustomAttributes;
using MonoFSM.EditorExtension;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.VariableBinder;
using MonoFSM.VarRefOld;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    public interface IDropdownRef { }

    /// <summary>
    ///     宣告「這個 Var 的值由網路權威端決定」的擁有者（實作在 NetworkedVarSync 那側，
    ///     core 不依賴 Fusion）。掛上後非 StateAuthority 端的本地寫入會被 Var 自己擋掉。
    /// </summary>
    public interface IVarNetworkAuthority
    {
        /// <summary>未 spawn / 已 despawn / 單機時應回 true（＝放行本地寫入）。</summary>
        bool HasVarStateAuthority { get; }
    }

    //FIXME: 應該要繼承AbstractSourceValueRef
    public abstract class AbstractMonoVariable //Rename self?
        : AbstractDescriptionBehaviour,
            IGuidEntity,
            IName,
            IValueOfKey<VariableTag>,
            IOverrideHierarchyIcon,
            IConfigTypeProvider,
            IResetStateRestore,
            IDropdownRef,
            IValueGetter
    {
        protected override string DescriptionTag =>
            HasValueSource ? (IsValueSourceSettable ? "Ref" : "Getter") : "Var";

        //ValueSource 可寫回（例如 VarBoolRef 指向另一個 VarBool）時，這個變數是 Ref 而不是唯讀 Getter
        //SetValue 也是走 valueSource is IValueSettable<TType> 這條路，判斷依據要一致
        protected virtual bool IsValueSourceSettable => false;

#if UNITY_EDITOR
        [CompRef] [AutoChildren] DebugWorldSpaceLabel _debugWorldSpaceLabel;
#endif
        //FIXME: 什麼case需要parentVarEntity? 忘記了XD
        // [ShowIf(nameof(_parentVarEntity))] //有才顯示就好, 或是debugMode?

        //FIXME: 與其用parentEntity, 好像 是一個ValueSource -> GetVarFromEntity比較好？
        [PreviewInDebugMode]
        [AutoParent(includeSelf: false)] //不可以抓到自己！
        protected VarEntity _parentVarEntity; //我的parent如果有VarEntity, 去跟這個entity拿？

        //varref
        [PropertyOrder(-1)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [Header("Variable Reference, 從 Parent Entity 拿 Variable")]
        [ShowIf(nameof(HasParentVarEntity))]
        [ShowInInspector] //TODO; runtime才會知道？
        protected AbstractMonoVariable varRef => _parentVarEntity?.Value?.GetVar(_varTag);

        //varRef 解不到時指出這條鏈斷在哪一段。
        //整條鏈上只有 MonoDict 的 not-prepared 會出聲，其餘（AutoParent 沒解到 / CurrentEntity 為 null /
        //VariableFolder 為 null / entity 沒有這顆 var）全是靜默 null，
        //寫入會靜默 fallback 成寫進 proxy 自己 —— byWhoQueue 看起來有寫成功，目標 entity 卻毫無反應。
        //只在失敗路徑才組字串，正常路徑不呼叫、不付 GC 代價。
        [ShowInDebugMode]
        public string VarRefFailureReason
        {
            get
            {
                if (_parentVarEntity == null)
                    return "_parentVarEntity 為 null（AutoParent 沒解到，或 parent 上沒有 VarEntity）";
                var entity = _parentVarEntity.Value;
                if (entity == null)
                    return $"[{_parentVarEntity.name}].Value 為 null（foreach 沒在迭代中，或 list 該格是空的）";
                if (_varTag == null)
                    return "_varTag 沒設，無從查表";
                if (entity.GetVar(_varTag) == null)
                    return $"entity [{entity.name}] 身上找不到 {_varTag.name}"
                           + "（VariableFolder 為 null，或 folder 裡沒有這顆 var）";
                return null;
            }
        }

        //ver reference?

        [ShowInInspector]
        public bool HasParentVarEntity
        {
            get
            {
                //如果是null這個會很白痴耶
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_parentVarEntity));
                // this.EnsureComponentInParent(ref _parentVarEntity, false, false);
                return _parentVarEntity != null;
            }
        }

        //bool isLocalVar? 不在folder下 || HasParentVar
        public bool HasParentVar => GetComponentInParent<AbstractMonoVariable>() != null;
#if UNITY_EDITOR
        public string IconName { get; }
        public bool IsDrawingIcon => CustomIcon != null;

        public Texture2D CustomIcon =>
            EditorGUIUtility.ObjectContent(null, GetType()).image as Texture2D; //雞掰！

        //Variable 專屬的反射查找（VariableReferenceWindow）；泛用的 Find References 已上移至 AbstractDescriptionBehaviour
        // [Button("Find Variable References"), PropertyOrder(-100)]
        // private void FindVariableReferences()
        // {
        //     // 透過反射呼叫 Editor Window，避免 Runtime 直接引用 Editor namespace
        //     var windowType = Type.GetType(
        //         "MonoFSM.Editor.VariableReferenceSystem.VariableReferenceWindow, MonoFSM.Core.Editor");
        //     if (windowType != null)
        //     {
        //         var method = windowType.GetMethod("ShowWindowWithVariable",
        //             BindingFlags.Public | BindingFlags.Static);
        //         method?.Invoke(null, new object[] { this });
        //     }
        //     else
        //     {
        //         Debug.LogWarning("VariableReferenceWindow not found. Please ensure MonoFSM.Core.Editor assembly is loaded.");
        //     }
        // }
#endif

        //FIXME: 這個不能被Debug「看」，不好用... AddListener 的形式比較好
        // private UnityAction OnValueChangedRaw; //任何數值改變就通知, UI有用到很重要 //override?

        protected HashSet<IVarChangedListener> _dataChangedListeners; //有誰有用我，binder綁一下
        [CompRef] [AutoChildren] public OnValueChangedHandler _valueChangedHandler;
        public abstract void ClearValue();

        //fuck!?

        //倒著，事件鏈超難trace
        public virtual void OnValueChanged() //FIXME: SetValue後要call 但會有boxing問題不寫在這？
        {
            if (!Application.isPlaying)
                return;
            if (_dataChangedListeners != null)
                foreach (var item in _dataChangedListeners)
                    item.OnVarChanged(this);
            _valueChangedHandler?.EventHandle();
            // OnValueChangedRaw?.Invoke();
            // Debug.Log("OnValueChanged", this);
        }

        public void AddListener(IVarChangedListener target)
        {
            if (_dataChangedListeners == null)
                _dataChangedListeners = new HashSet<IVarChangedListener>();
            _dataChangedListeners.Add(target);
        }

        // ===================== 網路權威 gate =====================
        //
        // 「權威在誰身上」是 Var 的性質，不是寫入者的性質：一旦這個 Var 被 NetworkedVarSync
        // 收進同步清單，值就由 StateAuthority 端決定。非 SA 端的本地寫入（FSM Action、Ability…）
        // 若照樣生效，會跟每 tick 讀回的權威值互相打架，表現為數值/UI 抖動。
        // 過去靠在每個 handler 勾 _stateAuthorityOnly 來擋，等於把同一件事重複標註在幾十個
        // 寫入點上，漏一個就抖，而且散在 prefab 裡看不出來。改成由 Var 自己擋，規則只剩一條。

        [NonSerialized] private IVarNetworkAuthority _netAuthority;

        [ShowInDebugMode] public bool IsNetworkAuthorityOwned => _netAuthority != null;

        /// <summary>由 NetworkedVarSync 在 Spawned / refetch 時注入（idempotent）。</summary>
        public void SetNetworkAuthorityOwner(IVarNetworkAuthority owner) => _netAuthority = owner;

        /// <summary>非 SA 端要擋掉本地寫入。未被任何 sync 認領（單機、編輯器）一律放行。</summary>
        protected bool IsLocalWriteBlockedByNetwork =>
            _netAuthority != null && !_netAuthority.HasVarStateAuthority;

        //被擋掉的寫入者，up peek / Inspector 看得到是誰想寫，不必逐個翻 prefab
        [ShowInDebugMode] [NonSerialized] public Object _lastNetworkBlockedSetter;

        [ShowInDebugMode] [NonSerialized] public float _lastNetworkBlockedTime = -1f;

        [Conditional("UNITY_EDITOR")]
        protected void RecordNetworkBlocked(Object byWho)
        {
            _lastNetworkBlockedSetter = byWho;
            _lastNetworkBlockedTime = Time.time;
            this.Log("SetValue blocked: 非 StateAuthority 端不可寫入已同步的 Var", byWho, this);
        }

        public void RemoveListener(IVarChangedListener target)
        {
            _dataChangedListeners?.Remove(target);
        }

        [ShowInDebugMode]
        [AutoParent]
        protected VariableFolder _variableFolder;

        // [Button]
        private void UpdateTag()
        {
            if (_varTag != null)
            {
                // Debug.Log($"Set _varTag:{_varTag} _variableType  {GetType()}", _varTag);

                //要怎麼找到對應的variable tag...要有一個dict可以找hmm
                _varTag._variableType.SetType(GetType());

                //如果有了不該蓋掉？如果改型別了呢？還是要看有沒有繼承關係？
                //FIXME: BaseFilterType應該要改？
                if (_varTag.HasOverrideValueFilterType == false)
                {
                    Debug.Log($"Set _varTag:{_varTag} ValueFilterType  {ValueType}", _varTag);
                    _varTag._valueFilterType.SetType(ValueType);
                }
            }

            // Debug.Log("Tag Changed");
            //variable folder refresh
            _variableFolder = GetComponentInParent<VariableFolder>();
            if (_variableFolder)
                _variableFolder.Refresh();
#if UNITY_EDITOR
            if (_varTag)
                EditorUtility.SetDirty(_varTag);
#endif
        }

        //         [Button("建立 ValueProvider Reference")]
        //         private void CreateValueProvider()
        //         {
        // #if UNITY_EDITOR
        //             if (_varTag == null)
        //             {
        //                 Debug.LogError("請先設定變數標籤 (VarTag) 才能建立 ValueProvider", this);
        //                 return;
        //             }
        //
        //             // 加入 ValueProvider 組件
        //             var valueProvider = gameObject.TryGetCompOrAdd<ValueProvider>();
        //
        //             valueProvider.DropDownVarTag = _varTag; //直接設定
        //
        //             // 設定 ValueProvider 的 EntityProvider
        //             valueProvider._entityProvider = GetComponentInParent<ParentEntityProvider>();
        //             // 標記為 dirty 以確保儲存
        //             EditorUtility.SetDirty(valueProvider);
        //
        // #else
        //             Debug.LogWarning("此功能僅在編輯器模式下可用");
        // #endif
        //         }

        //         [Button("建立 ValueProvider Reference In Children")]
        //         private void CreateValueProviderInChildren()
        //         {
        // #if UNITY_EDITOR
        //             if (_varTag == null)
        //             {
        //                 Debug.LogError("請先設定變數標籤 (VarTag) 才能建立 ValueProvider", this);
        //                 return;
        //             }
        //
        //             // 加入 ValueProvider 組件
        //             var valueProvider = gameObject.AddChildrenComponent<ValueProvider>("provider");
        //
        //             valueProvider.DropDownVarTag = _varTag; //直接設定
        //
        //             // 設定 ValueProvider 的 EntityProvider
        //             valueProvider._entityProvider = GetComponentInParent<ParentEntityProvider>();
        //             // 標記為 dirty 以確保儲存
        //             EditorUtility.SetDirty(valueProvider);
        //
        // #else
        //             Debug.LogWarning("此功能僅在編輯器模式下可用");
        // #endif
        //         }

        //proxy variable or local variable;
        //FIXME: 為什麼_variableFolder要hide?
        [ShowInDebugMode]
        // protected bool IsHidingVarTag => _variableFolder == null && HasParentVarEntity == false; //local var就失敗耶...hmm
        protected bool IsHidingVarTag =>
            _variableFolder == null && HasValueSource; //local var就失敗耶...hmm

        protected bool IsHidingDefaultValue =>
            HasValueSource || HasParentVarEntity || _variableFolder == null;

        //有 parent entity 但沒設 varTag → proxy lookup 不可能成立
        // [ShowInInspector]
        // [PropertyOrder(-2)]

        protected bool IsMissingVarTagForProxy => HasParentVarEntity && _varTag == null;

        //是一種Object Member的概念？
        [InfoBox(
            "已設定 Parent VarEntity 作為 proxy 來源，但缺少 VarTag。請設定 VarTag 才能從 parent entity 找到對應 variable。",
            InfoMessageType.Error,
            VisibleIf = nameof(IsMissingVarTagForProxy))]
        [HideIf(nameof(IsHidingVarTag))] //FIXME: 什麼時候算是localvariable?
        [FormerlySerializedAs("varTag")]
        // [MCPExtractable]
        [OnValueChanged(nameof(UpdateTag))]
        [Header("變數名稱")]
        [PropertyOrder(-1)]
        // [Required]
        [SOConfig("VariableType", nameof(CreateTagPostProcess))]
        public VariableTag _varTag; //直接看當下是什麼就可以 好像可以再往下抽？ ValueContainer? , readonly => Config, settable

#if UNITY_EDITOR

        #region GameData config 覆寫提示（editor-only）

        //這顆 Var 的 _varTag 若出現在同 VariableFolder 的 GameDataConfigInjector 所綁 GameData 的 config 表裡，
        //ResetStart 會把本地值蓋掉，inspector 上必須看得出來，不然改了 prefab 值卻沒反應會很難查。
        private double _lastConfigHintCheckTime;
        private bool _isConfigOverridden;
        private bool _isConfigSkipped;
        private string _configHintMessage;
        private GameDataConfigInjector _cachedConfigInjector;

        //inspector 每幀都會問，掃子樹的成本要節流
        private void RefreshConfigOverrideHint()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastConfigHintCheckTime < 0.5)
                return;
            _lastConfigHintCheckTime = now;

            _isConfigOverridden = false;
            _isConfigSkipped = false;
            _configHintMessage = null;

            if (_varTag == null)
                return;

            var folder = GetComponentInParent<VariableFolder>(true);
            if (folder == null)
                return;

            //injector 可以掛在 folder 本身或它底下任一節點
            if (_cachedConfigInjector == null)
                _cachedConfigInjector = folder.GetComponentInChildren<GameDataConfigInjector>(true);
            var injector = _cachedConfigInjector;
            if (injector == null)
                return;

            var data = injector.EditorBoundGameData;
            if (data == null)
                return;

            var isFloatVar = this is VarFloat;
            var isObjVar = this is MonoFSM.Core.Variable.VarMonoObj;
            var hasConfig = isFloatVar
                ? data.HasConfig(_varTag)
                : isObjVar && data.HasObjConfig(_varTag);
            if (!hasConfig)
                return;

            if (injector.IsTagSkipped(_varTag))
            {
                _isConfigSkipped = true;
                _configHintMessage =
                    $"本地值優先：GameData「{data.name}」有 {_varTag.name} 的 config，但已列在 {injector.name} 的 skipTags。";
                return;
            }

            _isConfigOverridden = true;
            if (isFloatVar && data.TryGetConfig(_varTag, out var floatValue))
                _configHintMessage =
                    $"ResetStart 會被 GameData「{data.name}」的 config 覆寫成 {floatValue}，這裡填的本地值不會生效。";
            else if (isObjVar && data.TryGetObjConfig(_varTag, out var objValue))
                _configHintMessage =
                    $"ResetStart 會被 GameData「{data.name}」的 config 覆寫成 "
                    + $"{(objValue != null ? objValue.name : "null")}，這裡填的本地值不會生效。";
        }

        private bool IsOverriddenByGameDataConfig
        {
            get
            {
                RefreshConfigOverrideHint();
                return _isConfigOverridden;
            }
        }

        private bool IsGameDataConfigSkipped
        {
            get
            {
                RefreshConfigOverrideHint();
                return _isConfigSkipped;
            }
        }

        private string ConfigOverrideHintMessage => _configHintMessage;

        //空字串當 InfoBox 的掛點：訊息由 InfoBox 畫，property 自己不佔版面
        [PropertyOrder(-2)]
        [ShowInInspector]
        [HideLabel]
        [DisplayAsString]
        [InfoBox("$" + nameof(ConfigOverrideHintMessage), InfoMessageType.Warning,
            VisibleIf = nameof(IsOverriddenByGameDataConfig))]
        [InfoBox("$" + nameof(ConfigOverrideHintMessage), InfoMessageType.Info,
            VisibleIf = nameof(IsGameDataConfigSkipped))]
        private string ConfigHintAnchor => "";

        [PropertyOrder(-2)]
        [ShowIf(nameof(IsOverriddenByGameDataConfig))]
        [Button("加入 skipTags（保留本地值）", ButtonSizes.Small)]
        private void AddToGameDataConfigSkipTags()
        {
            if (_cachedConfigInjector == null || _varTag == null)
                return;
            _cachedConfigInjector.EditorAddSkipTag(_varTag);
            _lastConfigHintCheckTime = 0; //強制下次重查
        }

        [PropertyOrder(-2)]
        [ShowIf(nameof(IsGameDataConfigSkipped))]
        [Button("從 skipTags 移除（改吃 GameData config）", ButtonSizes.Small)]
        private void RemoveFromGameDataConfigSkipTags()
        {
            if (_cachedConfigInjector == null || _varTag == null)
                return;
            _cachedConfigInjector.EditorRemoveSkipTag(_varTag);
            _lastConfigHintCheckTime = 0;
        }

        #endregion

#endif

        protected void CreateTagPostProcess()
        {
        }

        public T1 Get<T1>()
        {
            return GetValue<T1>();
        }

        public abstract void SetRaw<T1>(T1 value, Object byWho); //這個還是不太好，會有casting問題？

        public virtual Type ValueType => _varTag.ValueType; //遞回了ㄅ？

        //FIXME: 好亂喔QQ 好難trace
        // public abstract object objectValue { get; } //不好？generic value?

        public abstract T GetValue<T>();
        // {
        //     //FIXME: 很不好耶
        //     var value = objectValue;
        //     if (value == null)
        //         return default;
        //     try
        //     {
        //         return (T)value;
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"Cannot cast {value} to {typeof(T)}", this);
        //         return default;
        //     }
        // }

        // private readonly HashSet<Object> byWhoHashSet = new();
        // [ShowInDebugMode] public List<Object> byWhoList => byWhoHashSet.ToList();


        //FIXME: 不一定是struct的？

#if UNITY_EDITOR
        [ShowInDebugMode]
        private Queue<SetValueExecutionData> _byWhoQueue = new(); //沒有人清，resetrestore要清掉嗎？先不要好了

        public struct NetworkTickSnapshot
        {
            public int _tick;
            public bool _isForward;
            public bool _isResim;
        }

        // 由網路模組（如 MonoFSM.Fusion2）在 BeforeTick / FUN 時 push 進來；MonoFSM Core 不依賴 Fusion。
        // 沒有 runner 時為 null。push 模式比 pull (掃 NetworkRunner.Instances) 便宜。
        public static NetworkTickSnapshot? _networkTickSnapshot;

        [Serializable]
        public struct SetValueExecutionData
        {
            //FIXME 這個 object type會gc
            public object _value; //可能被attribute processor給處理到，好像有點太過侵入？
            public Object _byWho;
            public float _time;
            public int _tick;        // NetworkRunner.Tick（沒有 runner 時為 -1）
            public bool _hasNetwork; // 是否有抓到 runner
            public bool _isForward;  // Runner.IsForward
            public bool _isResim;    // Runner.IsResimulation
            public string _reason; //記錄 set 的原因
            public string _stackTrace; //完整的 call stack

            [Button]
            void LogStackTrace()
            {
                var net = _hasNetwork
                    ? $"tick={_tick} forward={_isForward} resim={_isResim}"
                    : "no-runner";
                Debug.Log($"[{_time:F2}s | {net}] {_reason}\n{_stackTrace}", _byWho);
            }
        }

        // [Button("Log Set History"), ShowInDebugMode]
        // private void LogSetHistory()
        // {
        //     if (_byWhoQueue == null || _byWhoQueue.Count == 0)
        //     {
        //         Debug.Log($"[{name}] No set history recorded.", this);
        //         return;
        //     }
        //
        //     var sb = new System.Text.StringBuilder();
        //     sb.AppendLine($"=== [{name}] Set History ({_byWhoQueue.Count} records) ===");
        //
        //     int index = 0;
        //     foreach (var data in _byWhoQueue)
        //     {
        //         sb.AppendLine($"\n--- #{index} @ {data._time:F2}s ---");
        //         sb.AppendLine($"Value: {data._value}");
        //         sb.AppendLine($"ByWho: {(data._byWho != null ? data._byWho.name : "null")}");
        //         if (!string.IsNullOrEmpty(data._reason))
        //             sb.AppendLine($"Reason: {data._reason}");
        //         sb.AppendLine($"StackTrace:\n{data._stackTrace}");
        //         index++;
        //     }
        //
        //     Debug.Log(sb.ToString(), this);
        // }
#endif
        [SerializeField] private bool _isLogStackTrace = false;
        //FIXME: 太卡了
        [Conditional("UNITY_EDITOR")]
        protected void RecordSetbyWhoDebug<T>(Object byWho, T tempValue, string reason = null)
        {
#if UNITY_EDITOR
            if (!RuntimeDebugSetting.IsDebugMode)
                return;

            if (_byWhoQueue.Count > 10)
                _byWhoQueue.Dequeue(); //保持最新的10個

            var stackString = "_isLogStackTrace = false";
            if (_isLogStackTrace)
            {
                var stackTrace = new StackTrace(5, true);
                stackString = stackTrace.ToString();
            }
            var snap = _networkTickSnapshot;
            var byWhoData = new SetValueExecutionData
            {
                _value = tempValue,
                _byWho = byWho,
                _time = Time.time,
                _hasNetwork = snap.HasValue,
                _tick = snap?._tick ?? -1,
                _isForward = snap?._isForward ?? false,
                _isResim = snap?._isResim ?? false,
                _reason = reason,
                _stackTrace = stackString,
            };
            // return;
            //這個會gc, hmm



// #if UNITY_EDITOR
//             // 取得完整 call stack，跳過前 2 層 (RecordSetbyWho 和 SetValue)
//             var stackTrace = new System.Diagnostics.StackTrace(2, true);
//             var stackString = stackTrace.ToString();
//aW
//             // var logMessage = string.IsNullOrEmpty(reason)
//             //     ? $"[Variable] Set {tempValue} byWho {byWho}"
//             //     : $"[Variable] Set {tempValue} byWho {byWho} reason: {reason}";
//             // this.Log(logMessage + "\n" + stackString);
//
//             byWhoData._stackTrace = stackString;
//             _byWhoQueue.Enqueue(byWhoData);
            _byWhoQueue.Enqueue(byWhoData);
#endif
        }

        //abstract?
        public abstract void SetValueFromVar(AbstractMonoVariable source, Object byWho);

        protected AbstractMonoVariable GetProxyVarOrThis()
        {
            if (_parentVarEntity == null) return this; //用proxy
            if (_parentVarEntity != this)
            {
                Debug.Log("Proxy SetValue to parent entity", _parentVarEntity);
                var targetVar = _parentVarEntity.Value.GetVar(_varTag);
                if (targetVar == null)
                {
                    Debug.LogError(
                        $"Parent entity {_parentVarEntity.name} has no var {_varTag.name}",
                        this
                    );
                    return this;
                }

                if (targetVar == this)
                {
                    Debug.LogError(
                        "Variable's parent entity is self, possible misconfiguration.",
                        this
                    );
                    Debug.Break();
                    return this;
                }

                // targetVar.SetValue(value, byWho);

                return targetVar;
            }
            else
            {
                Debug.LogError(
                    "Variable's parent entity is self, possible misconfiguration.",
                    this
                );
            }

            Debug.Break();

            return this;
        }

        public bool Equals(AbstractSourceValueRef sourceValueRef)
        {
            if (sourceValueRef == null)
            {
                Debug.LogError("Equals: sourceValueRef is null", this);
                return false;
            }

            var type = sourceValueRef.ValueType;
            if (type == typeof(int))
                return Equals(sourceValueRef.GetValue<int>());
            if (type == typeof(float))
                return Equals(sourceValueRef.GetValue<float>());
            if (type == typeof(bool))
                return Equals(sourceValueRef.GetValue<bool>());
            if (type == typeof(string))
                return Equals(sourceValueRef.GetValue<string>());
            if (type == typeof(Vector3))
                return Equals(sourceValueRef.GetValue<Vector3>());
            if (typeof(Object).IsAssignableFrom(type))
                return Equals(sourceValueRef.GetValue<Object>());
            Debug.LogWarning($"Equals: Unsupported type {type}", this);
            return Equals(sourceValueRef.GetValue<object>());
        }

        public bool Equals<T>(T value)
        {
            var v = GetValue<T>();
            return EqualityComparer<T>.Default.Equals(v, value);
        }

        /// <summary>
        /// 比較此 Variable 與另一個 Variable 的 Value 是否相等。
        /// 這層沒有型別資訊，只能用 ValueType 判斷型別不同即不相等，相同型別走可能裝箱的後備路徑。
        /// 具型別的子類別 <see cref="TypedMonoVariable{T}" /> 會 override 走泛型比較，避免裝箱與轉型。
        /// </summary>
        public virtual bool EqualsVar(AbstractMonoVariable other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(other, this))
                return true;
            if (other.ValueType != ValueType)
                return false;
            //無型別參數的後備路徑（可能裝箱）
            return Equals(other.GetValue<object>());
        }

        public object GetProperty(string knownFieldName)
        {
            return GetPropertyCache(knownFieldName)?.Invoke(this);
        }

        public Dictionary<string, Func<AbstractMonoVariable, object>> _propertyCache = new();

        //GameFlagDescriptable有一樣的東西喔
        public Func<AbstractMonoVariable, object> GetPropertyCache(string propertyName)
        {
            if (_propertyCache.TryGetValue(propertyName, out var info))
                return info;

            var propertyInfo = GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            // Debug.Log($"Property {propertyName} found in {sourceObject.GetType()}", sourceObject);

            if (propertyInfo == null)
            {
                _propertyCache[propertyName] = null;
                //FIXME: 可能因為unknownData所以有可能會找不到 有點危險？
                // Debug.LogError($"Property {propertyName} not found in {GetType()}");
                return null;
            }

            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                Debug.LogError($"Property {propertyName} does not have a getter in {GetType()}");
                return null;
            }

            Func<AbstractMonoVariable, object> getMyProperty = (source) =>
                getMethod.Invoke(source, null);
            _propertyCache[propertyName] = getMyProperty;
            return getMyProperty;
        }

// #if UNITY_EDITOR
        // [Header("GameState 功能說明")]
        //FIXME: 整合 AbstractDescriptable??
        // [TextArea(1, 4)]
        // public string description;

        public override string Description => _varTag != null ? _varTag.name : ReformatedName;


        // set => description = value;

        //包進去override會爆掉捏
        public abstract string StringValue { get; }
// #endif

        public string Name => gameObject.name;
        public VariableTag Key => _varTag;

        //proxy / getter 型的值不在自己身上（local field 永遠是空的），問自己一定回 false，
        //接了防禦式 early return 的呼叫端會 100% 早退且沒有任何錯誤訊息。
        //有來源時「算不算有值」一律轉問來源，沒有來源才問自己的 IsLocalValueExist。
        [ShowInInspector] //FIXME: 這個show的話，可能會造成 value 重運算
        public virtual bool IsValueExist
        {
            get
            {
                //value-source / proxy 可能接成參照環（X 的 source 讀 Y、Y 又繞回 X），
                //沿 IsValueExist 遞迴下去會 StackOverflow → Unity 不寫 log 直接閃退。
                //同一顆 instance 重入時降級成 false，把環變成看得見的訊息而非 crash。
                if (_resolvingValueExist)
                {
                    Debug.LogError(
                        "IsValueExist re-entrant：value-source/proxy 接成參照環，已中止以避免 StackOverflow。請檢查接線。",
                        this);
                    return false;
                }

                _resolvingValueExist = true;
                try
                {
                    //proxy 優先：值的家在 parent entity 上那顆同 tag 的 var
                    if (HasParentVarEntity)
                    {
                        var proxy = varRef;
                        //entity 當下沒值（foreach 沒在迭代 / list 該格是空的）或對面沒有這個 tag 的 var
                        if (proxy == null || proxy == this)
                            return false;
                        return proxy.IsValueExist;
                    }

                    var source = valueSource;
                    if (source != null)
                        return source.IsValueExist;

                    return IsLocalValueExist;
                }
                finally
                {
                    _resolvingValueExist = false;
                }
            }
        }

        [NonSerialized] private bool _resolvingValueExist;

        /// <summary>
        ///     這顆變數「自己身上」的值算不算存在，由各型別定義空值語意（0 / 空字串 / null / Count 0…）。
        ///     只在沒有 valueSource 也沒有 parent VarEntity proxy 時才會被呼叫，不需要自己處理來源轉發。
        /// </summary>
        protected abstract bool IsLocalValueExist { get; }

        //value source 機制：所有變數共用（TypedMonoVariable / VarList 都繼承這套），
        //一致地撿任何 child IValueProvider（含 GetVarFromParentEntitySource）。
        [HideIf(nameof(HasProxySource))]
        [SerializeField]
        private bool _needValueSource = false;

        protected bool IsNeedValueSourceButNone() => _needValueSource && valueSource == null;

        [InfoBox("需要一個ValueProvider來提供數值", InfoMessageType.Error,
            VisibleIf = nameof(IsNeedValueSourceButNone))]
        [CompRef]
        [AutoChildren(DepthOneOnly = true, _isSelfInclude = true)]
        protected IValueProvider[] _valueSources;

        protected IValueProvider valueSource => GetActiveValueSource();

        protected IValueProvider GetActiveValueSource()
        {
            AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_valueSources));
            return ValueResolver.GetActiveValueSource(_valueSources, this);
        }

        //有ValueProvider或ParentVarEntity的值來源
        protected virtual bool HasValueSource
        {
            get
            {
                AutoAttributeManager.AutoReferenceFieldEditor(this, nameof(_valueSources));
                return ValueResolver.HasValueProvider(_valueSources);
            }
        }

        /// <summary>
        ///     清掉網路覆寫值，讓 CurrentValue 退回自己的 valueSource / localField。
        ///     由 NetworkedVarSync 在本地取得 StateAuthority 時呼叫（此後本地算的才是權威值）。
        /// </summary>
        public virtual void ClearNetworkOverride() { }

        //FIXME: 有value和有 source是兩回事吧？HasProxySource?
        [InfoBox(
            "此變數會使用 ValueProvider 或 Parent VarEntity 的值，無法設定預設值"
        )]
        [ShowInInspector]
        public virtual bool HasProxySource =>
            HasValueSource || (HasParentVarEntity);


        public VariableTag[] GetKeys()
        {
            return new[] { _varTag };
        }

        //FIXME: 好像不該 override renmae? 應該是 override Description
        protected override void Rename()
        {
            //FIXME: 直接把繼承來的邏輯override掉囉
            // base.Rename();
            UpdateTag();
            if (_varTag == null)
            {
                base.Rename();

                //FIXME: 自動改名的做法，從 field 的名字來 rename? ex: VarEntity下的VarFloat? 還是應該要繼續用tag?
                return;
            }

            var str = _varTag.name;
            if (_parentVarEntity != null)
                str = _parentVarEntity.name + "." + str;

            name = $"[{DescriptionTag}] {FormatName(str)}";
            RevertNameOverrideIfMatchesPrefab(gameObject);
        }

        public Type GetRestrictType()
        {
            return _varTag?.ValueFilterType;
        }

        public abstract void ResetStateRestore(bool IsHardReset);
        // public abstract void ResetToDefaultValue();
    }
}
