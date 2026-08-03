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

    //FIXME: 應該要繼承AbstractSourceValueRef
    public abstract class AbstractMonoVariable //Rename self?
        : AbstractDescriptionBehaviour,
            IGuidEntity,
            IName,
            IValueOfKey<VariableTag>,
            IOverrideHierarchyIcon,
            IBeforePrefabSaveCallbackReceiver,
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

        [ShowInInspector] //FIXME: 這個show的話，可能會造成 value 重運算
        public abstract bool IsValueExist { get; }

        //value source 機制：所有變數共用（TypedMonoVariable / VarList 都繼承這套），
        //一致地撿任何 child IValueProvider（含 GetVarFromParentEntitySource）。
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
