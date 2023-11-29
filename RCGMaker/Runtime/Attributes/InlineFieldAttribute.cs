using System;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    // [Title("InlineField")] 可以類似用ShowInPlayModeAttributeProcessor來把InlineFieldAttribute加上
    [InlineProperty]
    [HideLabel]
    [IncludeMyAttributes]
    [Title("@$property.NiceName")]
    public class InlineFieldAttribute : Attribute //serialized class會有小箭頭要expand
    {

        public InlineFieldAttribute(string title = null)
        {

        }
        //add title to the field
    }
}