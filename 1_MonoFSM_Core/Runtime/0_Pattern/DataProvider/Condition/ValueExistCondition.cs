using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.Condition
{
    /// <summary>
    /// FIXME 不對耶
    /// </summary>
    public class ValueExistCondition : AbstractConditionBehaviour
    {
        public override string Description => $"Value Exist: {_targetValueGetter?.Description}";

        [DropDownRef]
        [SerializeField]
        private AbstractGetter _targetValueGetter;
        protected override bool IsValid => _targetValueGetter.HasValue;
    }
}
