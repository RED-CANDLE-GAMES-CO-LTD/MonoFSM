using System;
using RCGFSMCore._0_Pattern.DataProvider.ComponentWrapper;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class FloatCompareCondition : AbstractConditionComp
    {
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
        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [BoxGroup("Simple Comparison")]
        [DropDownRef]
        public VarFloat leftValue;

        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [BoxGroup("Simple Comparison")]
        public Operator op;

        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [BoxGroup("Simple Comparison")]
        public bool useConstantForRightValue = true;

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
        [BoxGroup("Advanced Comparison")]
        private IFloatProvider[] _floatValueSourceArray = Array.Empty<IFloatProvider>();

        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [PreviewInInspector] 
        [BoxGroup("Advanced Comparison")]
        private float Value1 => _floatValueSourceArray is { Length: > 0 }
            ? _floatValueSourceArray[0].GetFloat()
            : 0;

        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        Operator opView => op;

        [ShowIf(nameof(comparisonMode), ComparisonMode.Advanced)]
        [PreviewInInspector]
        [BoxGroup("Advanced Comparison")]
        private float Value2 => _floatValueSourceArray is { Length: > 1 }
            ? _floatValueSourceArray[1].GetFloat()
            : 0;

        private void OnComparisonModeChanged()
        {
            // Optional: Convert between modes if needed
        }

        // Visualization helper for simple mode
        [ShowIf(nameof(comparisonMode), ComparisonMode.Simple)]
        [PreviewInInspector]
        [BoxGroup("Simple Comparison")]
        private string SimplePreview => $"{(leftValue != null ? leftValue.ToString() : "null")} {op} {(useConstantForRightValue ? rightConstantValue.ToString() : (rightValue != null ? rightValue.ToString() : "null"))}";

        protected override bool IsValid
        {
            get
            {
                if (comparisonMode == ComparisonMode.Simple)
                {
                    float left = leftValue != null ? leftValue.Value : 0;
                    float right = useConstantForRightValue ? rightConstantValue : 
                                 (rightValue != null ? rightValue.GetFloat() : 0);
                    
                    return CompareValues(left, right, op);
                }
                else
                {
                    // Advanced mode
                    return CompareValues(Value1, Value2, op);
                }
            }
        }

        private bool CompareValues(float value1, float value2, Operator op)
        {
            return op switch
            {
                Operator.Equals => value1 == value2,
                Operator.NotEqual => value1 != value2,
                Operator.GreaterThan => value1 > value2,
                Operator.LessThan => value1 < value2,
                Operator.GreaterThanOrEqual => value1 >= value2,
                Operator.LessThanOrEqual => value1 <= value2,
                _ => false
            };
        }
    }
}