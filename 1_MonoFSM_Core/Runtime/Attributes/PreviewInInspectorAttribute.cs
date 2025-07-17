using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Attributes
{
    [MeansImplicitUse]
    [IncludeMyAttributes]
    [ShowInInspector]
    [DisableIf("@true")]
    public class PreviewInInspectorAttribute : Attribute
    {
        //給private autoparent, auto children用的, 還是要直接processor下去？有些真的不需要preview就不加了
    }

    
    // [IncludeMyAttributes]
    // [Required]
    // [ShowInInspector]
    // [DisableIf("@true")]
    // public class RequiredInParentAttribute : Attribute //又沒辦法auto parent
    // {
    //     //給private autoparent, auto children用的, 還是要直接processor下去？有些真的不需要preview就不加了
    // }
}