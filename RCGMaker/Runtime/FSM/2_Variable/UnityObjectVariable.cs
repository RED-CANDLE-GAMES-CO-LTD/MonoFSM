using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [Searchable]
    public abstract class AbstractMonoReferenceVariable : AbstractMonoVariable
    {
        //FIXME: RawValue
        [PreviewInInspector] public abstract Object RawValue { get; set; }
        public abstract void ClearValue();
    }

    //FIXME: 這個好像不好...
    public class GenericUnityObjectVariable<T> : AbstractMonoReferenceVariable, ISettable<T>, ILevelResetStart,
        IObjectReference where T : Object
    {
        [Button]
        protected virtual void Rename()
        {
            var str = "[" + GetType().Name + "]";
            if (varTag != null)
                str += varTag.name;
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

        //FIXME: 可以用schema來收斂型別嗎？
        [FormerlySerializedAs("_siblingValue")]
        [Header("預設值")]
        [SerializeField]
        [DropDownRef(null, nameof(SiblingValueFilter))]
        private T _siblingDefaultValue;

        Type SiblingValueFilter()
        {
            if (varTag == null)
                return typeof(T);
            // Debug.Log("RestrictType is " + varTag._valueFilterType.RestrictType);
            return varTag._valueFilterType.RestrictType;
        }

        [Header("預設值")] [HideIf(nameof(_siblingDefaultValue))] [SerializeField]
        private T _defaultValue;


        private T DefaultValue => _siblingDefaultValue != null ? _siblingDefaultValue : _defaultValue;

        [PreviewInInspector] public T Value => _currentValue;

        public override Object RawValue
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
            _currentValue = value;
        }

        public void SetValue(object value, MonoBehaviour byWho = null)
        {
            _currentValue = value as T;
        }

        protected override void SetValueInternal<T1>(T1 value, Object byWho = null)
        {
            Debug.Log("Set value to " + value, this);
            _currentValue = value as T;
        }

        public override void ClearValue()
        {
            _currentValue = null;
        }

        public override GameFlagBase FinalData { get; }
        public override Type FinalDataType => RawValue != null ? RawValue.GetType() : null;

        public override object objectValue => RawValue;


        // public override Component objectValue => RawValue;


        public void LevelResetStart()
        {
            _currentValue = DefaultValue;
        }

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
    }

    //variable
    //Monobehaviour 包著一個變數
    //需要Generic嗎...好像算了
    public class UnityObjectVariable : GenericUnityObjectVariable<Component>
    {
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