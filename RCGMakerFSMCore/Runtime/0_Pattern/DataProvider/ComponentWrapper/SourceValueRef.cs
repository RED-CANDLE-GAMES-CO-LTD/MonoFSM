using RCGMaker.Core;
using UnityEngine;

namespace RCGMakerFSM.VarRef
{
    public class SourceValueRef : MonoBehaviour
    {
        [Component] [Auto] IConfigVar _configVar;
        public object GetValue()
        {
            return _configVar.GetValue();
        }
    }
}