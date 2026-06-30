using System;
using System.Collections.Generic;
using System.Threading;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoDebugSetting;
using MonoFSM.Core.Attributes;
using MonoFSM.EditorExtension;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Search;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    //primitive type和object type要拆開
    [Searchable]
    public abstract class GenericUnityObjectVariable<TValueType>
        : TypedMonoVariable<TValueType>,
            ISettable<TValueType>,
            ISceneStart,
            IResetStateRestore,
            IHierarchyValueInfo
        where TValueType : Object
    {
        public override string StringValue => Value.ToString();
        public override void SetRaw<T1>(T1 value, Object byWho)
        {
            Profiler.BeginSample("GenericUnityObjectVariable<TValueType>.SetRaw");
            if (value is TValueType tValue)
                SetValueInternal(tValue, byWho);
            Profiler.EndSample();
        }

        public override void SetValueFromVar(AbstractMonoVariable source, Object byWho)
        {
            if (varRef != null)
            {
                varRef.SetValueFromVar(source, byWho);
            }

            var value = source.GetValue<TValueType>();
            Debug.Log(
                $"SetValueFromVar {source.name} value:{value}, TValueType:{typeof(TValueType)}",
                this
            );
            SetValueInternal(value, byWho);
        }

        public override T GetValue<T>()
        {
            var v = Value;
            if (v == null)
                return default;

            if (Value is T tValue)
                return tValue;
            Debug.LogError(
                $"GetValue<T> typeof: {typeof(T)} failed, actual type is {typeof(TValueType)}",
                this
            );
            return default;
        }

        // //核心Setter?
        // protected void SetObjValueInternal<T>(T value, Object byWho)
        // {
        //     _currentValue = value as TValueType;
        //     RecordSetbyWho(byWho, _currentValue);
        //     OnValueChanged();
        // }

        // 遞迴檢查相關的靜態成員
        private static readonly ThreadLocal<int> _recursionDepth = new(() => 0);

        private static readonly ThreadLocal<
            HashSet<GenericUnityObjectVariable<TValueType>>
        > _visitedVariables = new(() => new HashSet<GenericUnityObjectVariable<TValueType>>());

        private const int MAX_RECURSION_DEPTH = 10;

        [CompRef] [Auto] private IVarValueSettingProcessor<TValueType> _beforeSetProcessor;

        public override bool IsValueExist => Value != null;
        [HideIf(nameof(HasProxySource))] public bool _isRuntimeOnly = false;

        protected override bool HasError()
        {
            // 先檢查 VarTag 的型別限制
            if (_varTag != null && _defaultValue != null)
            {
                var restrictType = _varTag.ValueFilterType; //還是覺得是Component?
                if (restrictType != null)
                {
                    var actualType = _defaultValue.GetType();
                    if (!restrictType.IsAssignableFrom(actualType))
                    {
                        _errorMessage =
                            $"型別不匹配: _defaultValue 型別為 {actualType.Name}，但 VarTag '{_varTag.name}' 限制型別為 {restrictType.Name}";
                        return true;
                    }
                    // else
                    // {
                    //     Debug.LogError(
                    //         $"[Type Mismatch Warning] Default value type {actualType.Name} does not match VarTag '{_varTag.name}' restriction type {restrictType.Name}",
                    //         this
                    //     );
                    // }
                }
            }

            return base.HasError(); // || (_isRuntimeOnly == false && _defaultValue == null);
        }

        //FIXME: 可以額外做filterType?
        // [DynamicType]

        protected bool HideDefaultValue()
        {
            return HasProxySource || _isRuntimeOnly;
        }

        protected virtual bool HasDefaultValueError() => false;
        protected virtual string DefaultValueErrorMessage() => string.Empty;

        //FIXME: hide時可能會有髒髒 data
        [HideIf(nameof(HideDefaultValue))]
        [InfoBox("$" + nameof(DefaultValueErrorMessage), InfoMessageType.Error,
            VisibleIf = nameof(HasDefaultValueError))]
        [Required]
        [SerializeField]
        [DropDownRef]
        [Header("預設值")]
        protected TValueType _defaultValue; //ConfigSettingValue? //只有VarMonoObj才需要？

        /// <summary>
        /// 保持原有的強型別Value屬性，但使用動態轉型
        /// </summary>
        [GUIColor(0.2f, 0.8f, 0.2f)]
        [PreviewInInspector]
        public TValueType Value //動態？ varTag的RestrictType 才決定型別？
        {
            get
            {
                // Debug.Log($"Accessing Value of {name}", this);
                // 檢查遞迴深度
                if (_recursionDepth.Value >= MAX_RECURSION_DEPTH)
                {
                    Debug.LogError(
                        $"[Stack Overflow Protection] Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached in variable: {name}",
                        this
                    );
                    Debug.Break();
                    return null;
                }

                // 檢查循環引用
                if (_visitedVariables.Value.Contains(this))
                {
                    Debug.LogError(
                        $"[Circular Reference Protection] Circular reference detected in variable: {name} obj:{_parentObj}",
                        this
                    );
                    Debug.Break();
                    return null;
                }

                // 進入遞迴保護區域
                _recursionDepth.Value++;
                _visitedVariables.Value.Add(this);

                try
                {
                    // Profiler.BeginSample("GetValueInternal", this);
                    var rst = GetValueInternal();
                    // Profiler.EndSample();
                    return rst;
                }
                finally
                {
                    // 確保清理狀態，即使發生異常也要執行
                    _recursionDepth.Value--;
                    _visitedVariables.Value.Remove(this);

                    // 如果回到最頂層，清理 visited set
                    if (_recursionDepth.Value == 0)
                        _visitedVariables.Value.Clear();
                }
            }
        }

        [ShowInDebugMode]
        private string _valueDebugStatus;

        /// <summary>
        ///     實際的 Value 獲取邏輯，從原本的 Value getter 分離出來
        /// </summary>
        private TValueType GetValueInternal()
        {
            // Debug.Log($"Getting value of {name}", this);
            //要擋掉嗎？那Editor Time就都不要顯示？
            // if (!Application.isPlaying)
            //     return DefaultValue;

            // value source（含自己掛的 child IValueProvider，如 GetVarFromParentEntitySource）優先於
            // _parentVarEntity proxy：避免 subtree 內有 VarEntity 時，所有後代 Var 被強制進 proxy 模式
            // 而吃不到自己的來源。proxy 保留為 fallback。
            if (HasValueSource)
            {
                _valueDebugStatus = "Resolving from ValueProvider";
                var rst = valueSource?.Get<TValueType>(); //Editor可以拿吧？
                return rst;
            }

            if (HasParentVarEntity)
            {
                _valueDebugStatus = "Resolving from ParentVarEntity";
                if (_parentVarEntity.Value == null)
                {
                    // Debug.LogError(
                    //     $"{name}'s ParentVarEntity is null, cannot resolve var '{_varTag}'",
                    //     this
                    // );
                    //editor time 沒有正常
                    _valueDebugStatus = "ParentVarEntity is null";
                    return null; //還沒有entity, 過掉？
                }

                // 檢查自我引用（保留原有檢查）
                if (_parentVarEntity == this || _parentVarEntity.Value.GetVar(_varTag) == this)
                {
                    _valueDebugStatus = "Self reference detected";
                    Debug.LogError("ParentVarEntity cannot be self", this);
                    Debug.Break();
                    return null;
                }
                else
                {
                    if (_varTag == null)
                    {
                        Debug.LogError(
                            $"{name} has ParentVarEntity but no VarTag to resolve variable",
                            this
                        );
                        _valueDebugStatus = "VarTag is null";
                        return null;
                    }
                    var targetVar = _parentVarEntity.Value.GetVar(_varTag);
                    if (targetVar == null)
                    {
                        _valueDebugStatus = "Target variable not found in ParentVarEntity";
                        Debug.LogError(
                            $"{name}'s ParentVarEntity has no var: '{_varTag}' folder:{_parentVarEntity.Value}",
                            _parentVarEntity.Value
                        );
                        // Debug.Break();
                        return null;
                    }

                    // 額外的循環引用檢查
                    if (targetVar is GenericUnityObjectVariable<TValueType> targetGenericVar)
                        if (_visitedVariables.Value.Contains(targetGenericVar))
                        {
                            Debug.LogError(
                                $"[Circular Reference Protection] Detected circular reference through ParentVarEntity chain: {name} -> {targetVar.name}",
                                this
                            );
                            Debug.Break();
                            return null;
                        }

                    if (RuntimeDebugSetting.IsDebugMode)
                        _valueDebugStatus = $"Resolved from ParentVarEntity: {targetVar.name}";
                    return targetVar.GetValue<TValueType>();
                }
            }

            if (Application.isPlaying == false)
            {
                _valueDebugStatus = "Using _DefaultValue (Edit Mode)";
                return _defaultValue;
            }

            if (_isRuntimeOnly)
                return _tempValue;
            else
            {
                _valueDebugStatus = "Using _DefaultValue (Runtime)";
                return _defaultValue;
            }
        }

        // public override Object RawValue => Value; //FIXME: 用Object?

        // public T Value => _currentValue;
        //green
        //FIXME: 不該？
        // [InlineEditor]
        // [ShowInDebugMode]
        // protected TValueType _currentValue; //要用ObjectField? 這樣才統一？ Object不可能做成GameFlag/Data?

        //所有人都不該set這個

        [PreviewInDebugMode]
        private TValueType _lastValue;

        [PreviewInDebugMode]
        private TValueType _lastNonNullValue;

        // public void SetValue(TValueType value, MonoBehaviour byWho = null)
        // {
        //     SetObjValueInternal(value, byWho);
        // }

        public void SetValue(TValueType value, Object byWho = null, string reason = null)
        {
            SetValueInternal(value, byWho, reason);
        }



        public override void CommitValue()
        {
            // Profiler.BeginSample("GenericUnityObjectVariable<TValueType>.CommitValue", this);
            // Profiler.BeginSample("GetValue");

            //FIXME: get only不開拿？
            _lastValue = Value;
            // Profiler.EndSample();
            if (_lastValue != null)
                _lastNonNullValue = _lastValue;
            // Profiler.EndSample();
        }

        //FIXME: 不該留這個API
        // public override void SetValue(object value, MonoBehaviour byWho) //這好蠢？
        // {
        //     //這個trace好討厭...又跑下去，然後再上來internal
        //     SetValue<TValueType>((TValueType)value, byWho);
        // }


        //開個後門
        public void SetOverrideDefaultValue(TValueType value, Object byWho)
        {
            _beforeSetProcessor?.BeforeSetValueCallback(value);
            _tempValue = value;
            _defaultValue = value;
            OnValueChanged();
#if UNITY_EDITOR
            _lastSetByWho = byWho;
#endif
        }

        [ShowIf(nameof(_isRuntimeOnly))] [ShowInInspector]
        TValueType _tempValue;
        //怎麼那麼多種...
        protected void SetValueInternal(TValueType value, Object byWho, string reason = null)
        {
            if (varRef != null)
            {
                varRef.SetRaw(value, byWho);
                return;
            }

            if (!_isRuntimeOnly) //寫個 force SetValue?
            {
                Debug.LogError("Cannot set value of a non-runtime-only variable", this);
                Debug.Break();
                return;
            }

            if (value == Value) //一樣不處理
                return;

            _beforeSetProcessor?.BeforeSetValueCallback(value);

            _tempValue = value;
            RecordSetbyWhoDebug(byWho, _tempValue, reason);
            // OnValueChanged?.Invoke(_currentValue); //多一個參數的版本
            OnValueChanged();

#if UNITY_EDITOR
            _lastSetByWho = byWho;
#endif
        }

#if UNITY_EDITOR
        [PreviewInDebugMode]
        private Object _lastSetByWho;
#endif

        public override void ClearValue()
        {
            SetValueInternal(null, this, "ClearValue");
            // _currentValue = null;
        }

        // public override GameFlagBase FinalData { get; }
        // public override Type FinalDataType => RawValue != null ? RawValue.GetType() : null; //指的是DescriptableData
        public override Type ValueType => typeof(TValueType);

        public void EnterSceneStart()
        {
            // if (_isRuntimeOnly)
            //     SetValueInternal(Value, this, "EnterSceneStart");
        }

        public override void ResetStateRestore(bool IsHardReset)
        {
            //FIXME: 應該要清掉嗎？
            if (_isRuntimeOnly && HasProxySource == false)
            {
                ClearValue();
            }
            // SetValueInternal(_defaultValue, this, "ResetStateRestore");
        }

        //FIXME: 和isConfig定位一樣？
        // [PropertyOrder(-1)]
        // [Header("避免關卡重置時清除資料")]
        // [SerializeField]
        // public bool _isConst; //

        //避免reset restore?
        public virtual string ValueInfo
        {
            get
            {
                try
                {
                    //現在hierarchy上會一直去狂拿，會有問題嗎？
                    var value = Value;
                    return value != null ? value.name : "null";
                }
                catch (Exception ex)
                    when (ex.Message.Contains("recursion") || ex.Message.Contains("circular"))
                {
                    return "[Recursion Error]";
                }
            }
        }

        public bool IsDrawingValueInfo => true;
    }
}
