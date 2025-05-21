using RCGMaker.Core;
using UnityEngine;

namespace RCGMakerFSM.VarRef
{
    /// <summary>
    /// 放在Children可以直接被Component Reference
    /// </summary>
    public class SourceValueRef : AbstractSourceValueRef
    {
      
    }

    public abstract class AbstractSourceValueRef : MonoBehaviour
    {
        [Component] [Auto] private IConfigVar _configVar; //什麼鬼命名，IValueProvider?

        public object GetValue()
        {
            return _configVar.GetValue();
        }

        public T GetValue<T>()
        {
            return _configVar.GetValue<T>();
        }

        public override string ToString()
        {
#if UNITY_EDITOR
            _configVar = GetComponent<IConfigVar>();
            if (_configVar == null) return "";
#endif
            return _configVar.GetDescription();
        }
    }
}