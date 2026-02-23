using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class VarFloatIsBoundCondition : AbstractConditionBehaviour
    {
        public override string Description => _varFloat != null
            ? _varFloat.name + " is " + (_boundType == BoundType.Max ? "max" : "min")
            : "null var";

        public enum BoundType
        {
            Max,
            Min
        }

        public BoundType _boundType;
        [SerializeField] [DropDownRef] VarFloat _varFloat;

        protected override bool IsValid => _varFloat != null &&
                                           (_boundType == BoundType.Max
                                               ? _varFloat.IsMax
                                               : _varFloat.IsMin);
    }
}
