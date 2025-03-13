using System;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class FloatCompareCondition : AbstractConditionComp
    {
        [Component] [AutoChildren] [PreviewInInspector]
        private IFloatProvider[] floatValueSourceArray = Array.Empty<IFloatProvider>();

        float Value1 => floatValueSourceArray[0].GetFloat();
        float Value2 => floatValueSourceArray[1].GetFloat();

        [SerializeReference] public IFloatProvider floatValueSource1;

        // public FloatValueRef floatValueSource1;
        public Operator op;

        // public FloatValueRef floatValueSource2;
        [SerializeReference] public IFloatProvider floatValueSource2;

        protected override bool IsValid
        {
            get
            {
                return op switch
                {
                    // Operator.Equals => floatValueSource1.Value == floatValueSource2.Value,
                    // Operator.NotEqual => floatValueSource1.Value != floatValueSource2.Value,
                    // Operator.GreaterThan => floatValueSource1.Value > floatValueSource2.Value,
                    // Operator.LessThan => floatValueSource1.Value < floatValueSource2.Value,
                    // Operator.GreaterThanOrEqual => floatValueSource1.Value >= floatValueSource2.Value,
                    // Operator.LessThanOrEqual => floatValueSource1.Value <= floatValueSource2.Value,
                    Operator.Equals => Value1 == Value2,
                    Operator.NotEqual => Value1 != Value2,
                    Operator.GreaterThan => Value1 > Value2,
                    Operator.LessThan => Value1 < Value2,
                    Operator.GreaterThanOrEqual => Value1 >= Value2,
                    Operator.LessThanOrEqual => Value1 <= Value2,
                    _ => false
                };
            }
        }
    }
}