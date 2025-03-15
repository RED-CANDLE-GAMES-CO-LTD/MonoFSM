using RCGMaker.Core;
using UnityEngine;

namespace RCGMakerFSM.RCGMakerFSMCore._0_Pattern.DataProvider.ComponentWrapper
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