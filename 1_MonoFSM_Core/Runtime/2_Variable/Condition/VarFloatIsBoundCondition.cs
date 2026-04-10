using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class VarFloatIsBoundCondition : AbstractConditionBehaviour
    {
        public override string Description => _varFloat != null
            ? _boundType == BoundType.Percentage
                ? _varFloat.name + " % " + ArithmeticHelper.OperatorDescription(_op) + " " + (_targetPercentage * 100f).ToString("F0") + "%"
                : _varFloat.name + " is " + (_boundType == BoundType.Max ? "max" : "min")
            : "null var";

        public enum BoundType
        {
            Max,
            Min,
            Percentage
        }

        public BoundType _boundType;
        [SerializeField] [DropDownRef] VarFloat _varFloat;

        [ShowIf(nameof(_boundType), BoundType.Percentage)]
        public Operator _op = Operator.GreaterThan;

        [ShowIf(nameof(_boundType), BoundType.Percentage)]
        [Range(0f, 1f)]
        public float _targetPercentage;

        protected override bool IsValid
        {
            get
            {
                if (_varFloat == null) return false;

                return _boundType switch
                {
                    BoundType.Max => _varFloat.IsMax,
                    BoundType.Min => _varFloat.IsMin,
                    BoundType.Percentage => ArithmeticHelper.CompareValues(_varFloat.Percentage, _targetPercentage, _op),
                    _ => false
                };
            }
        }
    }
}
