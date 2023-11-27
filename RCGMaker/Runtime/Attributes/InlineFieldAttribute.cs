using System;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    // [Title("InlineField")] 可以類似用ShowInPlayModeAttributeProcessor來把InlineFieldAttribute加上
    [InlineProperty]
    [HideLabel]
    [IncludeMyAttributes]
    public class InlineFieldAttribute : Attribute //serialized class會有小箭頭要expand
    {
        private string title;

        public InlineFieldAttribute(string title = null)
        {
            this.title = title;
        }
        //add title to the field
    }
}