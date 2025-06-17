using System;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Attributes
{
    [IncludeMyAttributes]
    [InlineProperty]
    [HideLabel]
    //這個好屌！！
    [Title("@$property.NiceName")] // [Title("InlineField")] 可以類似用ShowInPlayModeAttributeProcessor來把InlineFieldAttribute加上
    public class InlineFieldAttribute : Attribute //serialized class會有小箭頭要expand
    {
        public InlineFieldAttribute()
        {

        }
        //add title to the field
    }
}