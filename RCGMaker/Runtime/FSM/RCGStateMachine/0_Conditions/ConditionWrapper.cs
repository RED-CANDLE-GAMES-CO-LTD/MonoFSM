using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    //幫ICondition包一層，讓他可以在hierarchy裡面顯示
    public class ConditionWrapper : AbstractConditionComp
    {
        [Required] [PreviewInInspector] [AutoParent]
        ICondition condition;

        protected override bool IsValid => condition.IsValid;
    }
}