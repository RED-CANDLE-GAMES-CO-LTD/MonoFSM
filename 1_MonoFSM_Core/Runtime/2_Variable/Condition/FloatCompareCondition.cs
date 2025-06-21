using System;
using jerryee.UnityMCP;
using MonoFSM.Condition;
using Sirenix.OdinInspector;

using MonoFSM.DataProvider;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;

namespace MonoFSM.Variable.Condition
{
    public class FloatCompareCondition : NotifyConditionComp //這個可以監聽嗎？leftvalue?
    {
        //每種表達都可以用op做到，不需要invert的功能
        public override bool IsInvertResultOptionAvailable => false;

        // Comparison mode determines the UI and input approach
        public enum ComparisonMode
        {
            Simple,       // Direct: VarFloat vs literal or VarFloat vs VarFloat
            Advanced      // Full flexibility with IFloatProvider components
        }

        [InfoBox("Simple mode offers a cleaner interface. Advanced mode allows full flexibility with IFloatProvider components.")]
        [OnValueChanged(nameof(OnComparisonModeChanged))]
        public ComparisonMode comparisonMode = ComparisonMode.Simple;

        // Simple mode properties
        [MCPExtractable]
        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [BoxGroup("Simple Comparison")]
        [DropDownRef]
        public VarFloat leftValue; //varint被排擠...

        // [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [MCPExtractable]
        [BoxGroup("Simple Comparison")]
        public Operator op; //怎麼assign enum?

        [MCPExtractable]
        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [BoxGroup("Simple Comparison")]
        public bool useConstantForRightValue = true;

        [MCPExtractable]
        [ShowIf("@comparisonMode == ComparisonMode.Simple && useConstantForRightValue")]
        [BoxGroup("Simple Comparison")]
        public float rightConstantValue;

        [ShowIf("@comparisonMode == ComparisonMode.Simple && !useConstantForRightValue")]
        [BoxGroup("Simple Comparison")]
        public VarFloatProviderRef rightValue;

        // Advanced mode properties - using components
        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [Component]
        [AutoChildren]
        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")] //editor time不會auto...好煩, 還是serialized field比較好?
        private IFloatProvider[] _floatValueSourceArray = Array.Empty<IFloatProvider>();

        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [PreviewInInspector] 
        [BoxGroup("Advanced Comparison")]
        private float Value1 =>  _floatValueSourceArray is { Length: > 0 }
            ? _floatValueSourceArray[0].Value
            : 0;

        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        Operator opView => op;

        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        private float Value2 => _floatValueSourceArray is { Length: > 1 }
            ? _floatValueSourceArray[1].Value
            : 0;

        private void OnComparisonModeChanged()
        {
            // Optional: Convert between modes if needed
        }

        // Visualization helper for simple mode
        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [PreviewInInspector]
        [BoxGroup("Simple Comparison")]
        private string SimplePreview => $"{(leftValue != null ? leftValue.name : "null")} {op} {(useConstantForRightValue ? rightConstantValue.ToString() : (rightValue != null ? rightValue.name : "null"))}";

        public override string Description => comparisonMode == ComparisonMode.Simple
            ? SimplePreview
            : $"{_floatValueSourceArray[0].Description} {op} {_floatValueSourceArray[1].Description}";

        protected override bool IsValid
        {
            get
            {
                if (comparisonMode == ComparisonMode.Simple)
                {
                    float left = leftValue != null ? leftValue.Value : 0;
                    float right = useConstantForRightValue ? rightConstantValue :
                        rightValue != null ? rightValue.Value : 0;
                    
                    return ArithmeticHelper.CompareValues(left, right, op);
                }
                else
                {
                    // Advanced mode
                    return ArithmeticHelper.CompareValues(Value1, Value2, op);
                }
            }
        }

        //監聽
        protected override IVariableField listenField => leftValue.Field;
    }
}