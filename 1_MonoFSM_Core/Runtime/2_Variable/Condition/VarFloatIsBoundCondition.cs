using MonoFSM.Condition;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class VarFloatIsBoundCondition : AbstractConditionBehaviour
    {
        [ConditionPreset("Float Max", Category = "Float", Priority = 100, ColorHex = "#FFB347")]
        private static void Preset_Max(VarFloatIsBoundCondition c)
        {
            c._boundType = BoundType.Max;
        }

        [ConditionPreset("Float Min", Category = "Float", Priority = 100, ColorHex = "#FFB347")]
        private static void Preset_Min(VarFloatIsBoundCondition c)
        {
            c._boundType = BoundType.Min;
        }


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

        [ShowIf(nameof(_boundType), BoundType.Percentage)]
        [ShowInInspector]
        private float CurrentPercentage => _varFloat != null ? _varFloat.Percentage : 0f;

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
