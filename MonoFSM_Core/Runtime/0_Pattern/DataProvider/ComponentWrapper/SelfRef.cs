using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using UnityEngine;

namespace MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class SelfRef : MonoBehaviour, IConfigVar
    {
        [PreviewInInspector] [AutoParent] private MonoDescriptable _descriptable;

        // [PreviewInInspector]
        public object GetValue()
        {
            return _descriptable;
        }

        public string GetDescription()
        {
            return "[Mono]Self";
        }
    }
}