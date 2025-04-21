using System;

using Sirenix.OdinInspector;

namespace MonoFSM.Variable.Attributes
{
    [Component]
    // [AutoChildren] //AutoAttribute沒辦法看懂ChildComp...
    [ShowInInspector]
    [DisableIf("@true")]
    [IncludeMyAttributes]
    public class CompRefAttribute : Attribute { }
}