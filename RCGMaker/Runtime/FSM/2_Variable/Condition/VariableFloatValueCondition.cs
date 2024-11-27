using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum Operator //FIXME: equality operator
{
    Equals, //==
    NotEqual, // !=
    GreaterThan, // >
    LessThan, // <
    GreaterThanOrEqual, // >=
    LessThanOrEqual // <=
    
}
namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
   
    public class VariableFloatValueCondition : AbstractConditionComp
    {
        protected override string nameDescription => name = "[Condition] " + variableFloat.name + " " + op + " " + targetValue;

        [DropDownRef] public VariableFloat variableFloat;
        public Operator op;
        public float targetValue;

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
                        return variableFloat.CurrentValue == targetValue;
                    case Operator.NotEqual:
                        return variableFloat.CurrentValue != targetValue;
                    case Operator.GreaterThan:
                        return variableFloat.CurrentValue > targetValue;
                    case Operator.LessThan:
                        return variableFloat.CurrentValue < targetValue;
                    case Operator.GreaterThanOrEqual:
                        return variableFloat.CurrentValue >= targetValue;
                    case Operator.LessThanOrEqual:
                        return variableFloat.CurrentValue <= targetValue;
                }

                return false;
            }
        }
    }
}