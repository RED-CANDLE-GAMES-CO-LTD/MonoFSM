using System;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;

namespace MonoFSM_Core.Runtime.Attributes
{
    [Component]
    // [AutoChildren] //AutoAttribute沒辦法看懂ChildComp...
    [ShowInInspector]
    [DisableIf("@true")]
    [IncludeMyAttributes]
    public class CompRefAttribute : Attribute
    {
    }
}