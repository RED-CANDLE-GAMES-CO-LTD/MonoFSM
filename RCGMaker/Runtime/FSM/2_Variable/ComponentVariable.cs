using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [Searchable]
    public abstract class AbstractObjectVariable : AbstractMonoVariable
    {
        //FIXME: 這個是多的嗎？
        [PreviewInInspector] public abstract Object RawValue { get; set; }
        public abstract void ClearValue();
    }

    //FIXME: 這個好像不好...
    public class GenericUnityObjectVariable<T> : AbstractObjectVariable, ISettable<T>, ILevelResetPrepare,
        IObjectReference where T : Object
    {
        public UnityAction<T> OnValueChanged;

        [Button]
        protected virtual void Rename()
        {
            var str = "";
            if (varTag != null)
                str += varTag.name;
            else
            {
                str += "[" + GetType().Name + "]";
            }

            name = str;
        }

        // protected virtual IEnumerable<Type> filter()
        // {
        //     var q = typeof(Object).Assembly.GetTypes();
        //     return q;
        // }
        // [TypeFilter("filter")]
        // IEnumerable<Object> _filter()
        // {
        //     return Resources.FindObjectsOfTypeAll(typeof(T));
        // }
        //
        // [ValueDropdown(nameof(_filter))]
        //FIXME: 只有UnityObjectField有用到這個...Data的用不到啊, 移到外面？ override DefaultValue?
        // [FormerlySerializedAs("_siblingValue")]
        // [Header("預設值")]
        // [SerializeField]
        // [DropDownRef(null, nameof(SiblingValueFilter))]
        // private T _siblingDefaultValue;

        Type SiblingValueFilter()
        {
            if (varTag == null)
                return typeof(T);
            // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
            return varTag._valueFilterType.RestrictType;
        }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        protected T _defaultValue;


        protected virtual T DefaultValue
        {
            get { return _defaultValue; }
            // set { _defaultValue = value; }
        }
        // _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;

        [PreviewInInspector] public T Value => _currentValue;

        // public override T1 GetValue<T1>() //寫這三小...
        // {
        //     // return base.GetValue<T1>();
        //     return _currentValue;
        // }

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
                _currentValue = value as T;
                Debug.Log("Set CurrentValue to " + value, this);
            }
        }

        // public T Value => _currentValue;
        //green
        [GUIColor(0.2f, 0.8f, 0.2f)] [PreviewInInspector]
        // [InlineEditor]
        private T _currentValue; //要用ObjectField? 這樣才統一？

        [PreviewInInspector] private T _lastValue;

        [PreviewInInspector] private T _lastNonNullValue;

        public void CommitValue()
        {
            _lastValue = _currentValue;
            if (_currentValue != null)
                _lastNonNullValue = _currentValue;
        }

        public void SetValue(T value, MonoBehaviour byWho = null)
        {
            SetValue<T>(value, byWho);
        }

        public void SetValue(object value, MonoBehaviour byWho = null)
        {
            SetValue<T>((T)value, byWho);
        }

        //怎麼那麼多種...
        protected override void SetValueInternal<T1>(T1 value, Object byWho = null)
        {
            Debug.Log("Set value to " + value, this);
            _currentValue = value as T;
            OnValueChanged?.Invoke(_currentValue);
        }

        public override void ClearValue()
        {
            SetValue(null);
            // _currentValue = null;
        }

        public override GameFlagBase FinalData { get; }
        public override Type FinalDataType => RawValue != null ? RawValue.GetType() : null;
        public override Type ValueType => typeof(T);

        public override object objectValue => _currentValue;
        // public override Component objectValue => RawValue;


        //FIXME: Editor用的...EditorObjectValue?
        public Object EditorValue
        {
            get => DefaultValue;
            set
            {
                _defaultValue = value as T;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public Type ObjectType => typeof(T);

        public void LevelResetPrepareRuntimeData()
        {
            _currentValue = DefaultValue;
        }
    }

    //variable
    //Monobehaviour 包著一個變數
    //需要Generic嗎...好像算了
    public class ComponentVariable : GenericUnityObjectVariable<Component>
    {
        [FormerlySerializedAs("_siblingValue")]
        [Header("預設值")]
        [SerializeField]
        [DropDownRef(null, nameof(SiblingValueFilter))]
        private Component _siblingDefaultValue;

        Type SiblingValueFilter()
        {
            if (varTag == null)
                return typeof(Component);
            // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
            return varTag._valueFilterType.RestrictType;
        }

        //FIXME: 繼承時想要加更多attribute
        // [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        // protected Component _defaultValue;


        protected override Component DefaultValue =>
            _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;
        //FIXME: 把Variable直接丟到該模組上就好？
        // IEnumerable<Component> _filter()
        // {
        //     // var type = serializedType.Value;
        //     // if(type == null)
        //     //     return null;
        //     // return this.GetComponentsOfSibling<LevelRunner>(type);
        // }

        // [ValueDropdown(nameof(_filter))]
        // [SerializeField]
        // private Component DefaultValue;

        // [TypeDrawerSettings(BaseType = typeof(Component)), ShowInInspector]
        // public Type type; //FIXME: 要用string 回推 type?
    }
}