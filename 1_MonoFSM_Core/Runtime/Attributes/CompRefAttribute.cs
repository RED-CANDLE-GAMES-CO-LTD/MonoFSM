using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace MonoFSM.Variable.Attributes
{
    [Component]
    // [AutoChildren] //AutoAttribute沒辦法看懂ChildComp...
    [ShowInInspector]
    [DisableIf("@true")]
    [IncludeMyAttributes]
    [MeansImplicitUse]
    
    public class CompRefAttribute : Attribute { }
    //TODO: 限定型別？
}