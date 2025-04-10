using RCGMaker.Core;
using UnityEngine;

namespace RCGMakerFSM.VarRef
{
    /// <summary>
    /// 放在Children可以直接被Component Reference
    /// </summary>
    public class SourceValueRef : MonoBehaviour
    {
        [Component] [Auto] IConfigVar _configVar;
        public object GetValue()
        {
            return _configVar.GetValue();
        }
    }
}