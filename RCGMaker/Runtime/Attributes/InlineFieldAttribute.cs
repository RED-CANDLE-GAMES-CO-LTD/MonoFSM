using System;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    [InlineProperty]
    [HideLabel]
    [IncludeMyAttributes]
    public class InlineFieldAttribute : Attribute //serialized class會有小箭頭要expand
    {
    }
}