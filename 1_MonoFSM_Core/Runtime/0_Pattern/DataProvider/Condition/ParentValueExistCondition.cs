using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.DataProvider.Condition
{
    public class ParentValueExistCondition : AbstractConditionBehaviour
    {
        [Required] [PreviewInInspector] [AutoParent]
        private IValueProvider _parentValueProvider;

        protected override bool IsValid => _parentValueProvider is { IsValueExist: true };
    }
}