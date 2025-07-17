using System;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;
using UnityEngine.Serialization;


namespace MonoFSM.Variable
{
    [Searchable]
    public abstract class AbstractObjectVariable : AbstractMonoVariable
    {
        //FIXME: 這個是多的嗎？
        [PreviewInInspector] public abstract Object RawValue { get; set; } //不一定有Object可以returnㄅ？
        public abstract void ClearValue();
        // protected void SetValueExecution();
    }

    public abstract class GenericUnityObjectVariable<TValueType> : AbstractObjectVariable, ISettable<TValueType>,
        IResetStateRestore where TValueType : Object
    {
        public override bool IsValueExist => _currentValue != null;
        // public UnityAction<TValueType> OnValueChanged;
        //
        // //FIXME: 這個好ㄇ
        // public override void AddListener<T>(UnityAction<T> action)
        // {
        //     if (action is UnityAction<TValueType> typedAction)
        //         OnValueChanged += typedAction;
        //     else
        //         Debug.LogError("Action type mismatch. Expected UnityAction<TValueType>.", this);
        // }

        [Button]
        protected virtual void Rename()
        {
            var str = "";
            if (_varTag != null)
                str += _varTag.name;
            else
            {
                str += "[" + GetType().Name + "]";
            }

            name = str;
        }


        // Type SiblingValueFilter()
        // {
        //     if (varTag == null)
        //         return typeof(T);
        //     // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
        //     return varTag._valueFilterType.RestrictType;
        // }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))]
        [SerializeField] protected TValueType _defaultValue;


        protected virtual TValueType DefaultValue
        {
            get { return _defaultValue; }
            // set { _defaultValue = value; }
        }
        // _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;

        [PreviewInInspector]
        public TValueType Value
        {
            get
            {
                if (!Application.isPlaying)
                    return DefaultValue;
                return _currentValue;
            }
        }

        public override Object RawValue //FIXME: 用Object?
        {
            get
            {
                if (!Application.isPlaying)
                    return DefaultValue;
                return _currentValue;
            }
            set
            {
                _currentValue = value as TValueType;
                Debug.Log("Set CurrentValue to " + value, this);
            }
        }

        // public T Value => _currentValue;
        //green
        [GUIColor(0.2f, 0.8f, 0.2f)] [PreviewInInspector]
        // [InlineEditor]
        private TValueType _currentValue; //要用ObjectField? 這樣才統一？

        [PreviewInInspector] private TValueType _lastValue;

        [PreviewInInspector] private TValueType _lastNonNullValue;

        public void CommitValue()
        {
            _lastValue = _currentValue;
            if (_currentValue != null)
                _lastNonNullValue = _currentValue;
        }

        private void SetValueExecution(TValueType value, MonoBehaviour byWho = null)
        {
            SetValue<TValueType>(value, byWho);
        }

        public void SetValue(object value, MonoBehaviour byWho)
        {
            SetValue<TValueType>((TValueType)value, byWho);
        }

        public void SetValue(TValueType value, MonoBehaviour byWho)
        {
            SetValue<TValueType>(value, byWho);
        }

        //怎麼那麼多種...
        protected override void SetValueInternal<T1>(T1 value, Object byWho)
        {
            // Debug.Log("Set value to " + value, this);
            _currentValue = value as TValueType;
            // OnValueChanged?.Invoke(_currentValue); //多一個參數的版本
            OnValueChanged();
#if UNITY_EDITOR
            _lastSetByWho = byWho;
#endif
        }

#if UNITY_EDITOR
        [PreviewInDebugMode] private Object _lastSetByWho;
#endif

        public override void ClearValue()
        {
            SetValueExecution(null);
            // _currentValue = null;
        }

        // public override GameFlagBase FinalData { get; }
        // public override Type FinalDataType => RawValue != null ? RawValue.GetType() : null; //指的是DescriptableData
        public override Type ValueType => typeof(TValueType);

        public override object objectValue => _currentValue;
        // public override Component objectValue => RawValue;


        //FIXME: Editor用的...EditorObjectValue?
        public Object EditorValue
        {
            get => DefaultValue;
            set
            {
                _defaultValue = value as TValueType;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public Type ObjectType => typeof(TValueType);

        public void ResetStateRestore()
        {
            //這裡才做會不會太晚？
            if (_isConst)
                return;
            SetValueExecution(DefaultValue, this);
        }

        [FormerlySerializedAs("_isPreventReset")]
        [PropertyOrder(-1)]
        [Header("避免關卡重置時清除資料")]
        [SerializeField]
        public bool _isConst = false;
        //避免reset restore?

    }
}