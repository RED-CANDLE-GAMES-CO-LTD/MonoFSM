using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
        protected override string nameDescription => _monoVariableFloat
            ? name = "[Condition] " + _monoVariableFloat.name + " " + op + " " + targetValue
            : name = "[Condition]";

        [FormerlySerializedAs("variableFloat")] [DropDownRef]
        public VariableFloat _monoVariableFloat;

        public Operator op;
        public float targetValue;

        protected override bool isValid
        {
            get
            {
                if (_monoVariableFloat == null)
                {
                    // Debug.LogError("variableFloat is null", this);
                    return false;
                }

                switch (op)
                {
                    case Operator.Equals:
                        return _monoVariableFloat.CurrentValue == targetValue;
                    case Operator.NotEqual:
                        return _monoVariableFloat.CurrentValue != targetValue;
                    case Operator.GreaterThan:
                        return _monoVariableFloat.CurrentValue > targetValue;
                    case Operator.LessThan:
                        return _monoVariableFloat.CurrentValue < targetValue;
                    case Operator.GreaterThanOrEqual:
                        return _monoVariableFloat.CurrentValue >= targetValue;
                    case Operator.LessThanOrEqual:
                        return _monoVariableFloat.CurrentValue <= targetValue;
                }

                return false;
            }
        }
    }
}