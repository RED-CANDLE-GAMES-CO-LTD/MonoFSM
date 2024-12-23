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
    public abstract class AbstractReferenceVariable:AbstractVariable
    {
        //FIXME: RawValue
        [PreviewInInspector]
        public abstract Object RawValue { get; set; }
        public abstract void ClearValue();
    }
    
    //FIXME: 這個好像不好...
    public class GenericUnityObjectVariable<T>:AbstractReferenceVariable,ISettable<T>,ILevelResetStart,IObjectReference where T:Object
    {
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
        [SerializeField]
        private T DefaultValue;

        public T Value => _currentValue;
        public override Object RawValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value as T;
                Debug.Log("Set CurrentValue to " + value, this);
            } 
        }
        // public T Value => _currentValue;
        //green
        [GUIColor(0.2f, 0.8f, 0.2f)]
        [PreviewInInspector]
        // [InlineEditor]
        private T _currentValue; //要用ObjectField? 這樣才統一？
        [PreviewInInspector]
        private T _lastValue;

        [PreviewInInspector]
        private T _lastNonNullValue;
        public void CommitValue()
        {
            _lastValue = _currentValue;
            if(_currentValue != null)
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
                DefaultValue = value as T;
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