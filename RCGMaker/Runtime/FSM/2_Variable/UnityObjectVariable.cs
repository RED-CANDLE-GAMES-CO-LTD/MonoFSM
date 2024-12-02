using System;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [Searchable]
    public abstract class AbstractReferenceVariable:AbstractVariable
    {
        //FIXME: RawValue
        public abstract Object RawValue { get; set; }
        public abstract void ClearValue();
    }
    public class GenericUnityObjectVariable<T>:AbstractReferenceVariable,ILevelResetStart where T:Object
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
        [PreviewInInspector]
        [InlineEditor]
        private T _currentValue;
        [PreviewInInspector]
        private T _lastValue;

        [PreviewInInspector]
        private T _lastNonNullValue;
        public override void CommitValue()
        {
            _lastValue = _currentValue;
            if(_currentValue != null)
                _lastNonNullValue = _currentValue;
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
    }
    //variable
    //Monobehaviour 包著一個變數
    //需要Generic嗎...好像算了
    public class UnityObjectVariable:GenericUnityObjectVariable<Component>
    {
        IEnumerable<Component> _filter()
        {
            return this.GetComponentsOfSibling<LevelRunner>(type);
        }
        
        [ValueDropdown(nameof(_filter))]
        [SerializeField]
        private Component DefaultValue;

        [TypeDrawerSettings(BaseType = typeof(Component)),ShowInInspector]
        public Type type;
    }
}