using System;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    [IncludeMyAttributes]
    [ShowInInspector]
    [DisableInEditorMode]
    public class PreviewInInspectorAttribute : Attribute
    {
        //給private autoparent, auto children用的, 還是要直接processor下去？有些真的不需要preview就不加了
    }
}