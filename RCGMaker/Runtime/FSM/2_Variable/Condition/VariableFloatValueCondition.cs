using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum Operator //FIXME: equality operator
{
    Equals, //==
    NotEqual, // !=
    GreaterThan, // >
    LessThan, // <
    
}
namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
   
    public class VariableFloatValueCondition : AbstractConditionComp
    {
        private void OnValidate()
        {
            name = "[Condition] FloatValueCondition " + op.ToString();
        }

        [InlineEditor] [Required] public VariableFloat variableFloat;
        public float targetValue;
        public Operator op;

        protected override bool isValid
        {
            get
            {
                if (variableFloat == null)
                {
                    // Debug.LogError("variableFloat is null", this);
                    return false;
                }

                switch (op)
                {
                    case Operator.Equals:
                        return variableFloat.Value == targetValue;
                    case Operator.NotEqual:
                        return variableFloat.Value != targetValue;
                    case Operator.GreaterThan:
                        return variableFloat.Value > targetValue;
                    case Operator.LessThan:
                        return variableFloat.Value < targetValue;
                }

                return false;
            }
        }
    }
}