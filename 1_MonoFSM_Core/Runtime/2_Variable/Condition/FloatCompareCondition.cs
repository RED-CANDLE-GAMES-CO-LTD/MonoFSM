using System;
using MonoFSM.Condition;
using Sirenix.OdinInspector;

using MonoFSM.DataProvider;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;

namespace MonoFSM.Variable.Condition
{
    public class FloatCompareCondition : NotifyConditionBehaviour //這個可以監聽嗎？leftvalue?
    {
        public override bool IsInvertResultOptionAvailable => false;

        // Only keep Advanced mode
        // public enum ComparisonMode
        // {
        //     Advanced      // Full flexibility with IFloatProvider components
        // }

        // [InfoBox("Simple mode offers a cleaner interface. Advanced mode allows full flexibility with IFloatProvider components.")]
        // [OnValueChanged(nameof(OnComparisonModeChanged))]
        // public ComparisonMode comparisonMode = ComparisonMode.Advanced;

        // Advanced mode properties - using components
        [Component]
        [AutoChildren]
        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        private IValueProvider<float>[] _floatValueSourceArray = Array.Empty<IValueProvider<float>>();

        [PreviewInInspector] 
        [BoxGroup("Advanced Comparison")]
        private float Value1 =>  _floatValueSourceArray is { Length: > 0 }
            ? _floatValueSourceArray[0].Value
            : 0;

        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        Operator opView => op;

        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        private float Value2 => _floatValueSourceArray is { Length: > 1 }
            ? _floatValueSourceArray[1].Value
            : 0;

        // Operator for advanced mode
        public Operator op;

        private void OnComparisonModeChanged()
        {
            // Optional: Convert between modes if needed
        }

        public override string Description =>
            $"{_floatValueSourceArray[0].Description} {op} {_floatValueSourceArray[1].Description}";

        protected override bool IsValid =>
            // Advanced mode only
            ArithmeticHelper.CompareValues(Value1, Value2, op);

        //監聽
        protected override IVariableField listenField => null;
    }
}