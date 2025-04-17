using RCGMaker.Core;
using UnityEngine;

namespace RCGMakerFSM.VarRef
{
    /// <summary>
    /// 放在Children可以直接被Component Reference
    /// </summary>
    public class SourceValueRef : MonoBehaviour
    {
        [Component] [Auto] private IConfigVar _configVar;

        public object GetValue()
        {
            return _configVar.GetValue();
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