using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace MonoFSM.VarRef
{
    /// <summary>
    /// 放在Children可以直接被Component Reference
    /// </summary>
    public class SourceValueRef : AbstractSourceValueRef
    {
      
    }

    public abstract class AbstractSourceValueRef : MonoBehaviour
    {
        //如果有多個？避免？
        [Component] [Auto] private IValueProvider _valueProvider; //什麼鬼命名，IValueProvider?

        [PreviewInInspector] private object _previewLastValue;
        
        
        public object GetValue()
        {
            var value = _valueProvider.GetValue();
            //value processor?
#if UNITY_EDITOR
            _previewLastValue = value;
#endif
            return value;
        }

        public T GetValue<T>()
        {
            var value = _valueProvider.GetValue<T>();
#if UNITY_EDITOR
            _previewLastValue = value;
#endif
            return value;
        }

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