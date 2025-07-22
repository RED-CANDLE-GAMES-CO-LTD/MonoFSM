using System;
using System.Collections.Generic;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.VarRefOld
{
    public class GenericValue
    {
        public int intValue;
        public float floatValue;
        public Vector3 vector3Value;
        public Vector2 vector2Value;
        public string stringValue;
        public object objectValue;

        public GenericValue()
        {
        }

        public GenericValue(int intValue)
        {
            this.intValue = intValue;
        }

        public GenericValue(float floatValue)
        {
            this.floatValue = floatValue;
        }

        public GenericValue(Vector3 vector3Value)
        {
            this.vector3Value = vector3Value;
        }

        public GenericValue(Vector2 vector2Value)
        {
            this.vector2Value = vector2Value;
        }

        public GenericValue(string stringValue)
        {
            this.stringValue = stringValue;
        }

        public GenericValue(object objectValue)
        {
            this.objectValue = objectValue;
        }
    }
    
    /// <summary>
    /// 放在Children可以直接被Component Reference
    /// </summary>
    public class SourceValueRef : AbstractSourceValueRef
    {
        //好像要指定對象耶...身上一堆provider誰知道你要哪個值？
    }

    public abstract class AbstractSourceValueRef : AbstractDescriptionBehaviour
    {
        public bool Equals<T>(T value)
        {
            var v = GetValue<T>();
            return EqualityComparer<T>.Default.Equals(v, value);
        }
        //如果有多個？避免？
        //改成autoParent如何？
        //還要再多一層比較好？
        [Required] [CompRef] [Auto] private IValueProvider _valueProvider; //什麼鬼命名，IValueProvider?

#if UNITY_EDITOR
        //還要playmode版本？
        [ShowInDebugMode] private object _previewLastValue; // = new(); //這顆會boxing...
        // private object CurrentValue => _previewLastValue;
#endif
        public Type ValueType => _valueProvider.ValueType;

        public object objectValue => _valueProvider.Get<object>();
        
        public T GetValue<T>()
        {
            var value = _valueProvider.Get<T>();
#if UNITY_EDITOR
            _previewLastValue = value; //這顆會boxing...
#endif
            return value;
        }

        [PreviewInInspector] public override string Description => ToString();

        protected override string DescriptionTag => "Source Value";

        public override string ToString()
        {
#if UNITY_EDITOR
            _valueProvider = GetComponent<IValueProvider>();
            if (_valueProvider == null) return "";
#endif
            return _valueProvider.Description;
        }
    }
}