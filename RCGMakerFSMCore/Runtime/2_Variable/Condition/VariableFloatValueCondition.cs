using System;
using RCGMaker.Core.DataProvider;
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
    /// <summary>
    /// 和FloatCompareCondition重複？還是這個要做成簡單版？
    /// </summary>
    public class VariableFloatValueCondition : AbstractConditionComp
    {
        protected override string Description => _monoVarFloatProvider != null
            ? name = "[Condition] " + _monoVarFloatProvider.Description + " " + op + " " + targetValue
            : name = "[Condition]";

        // [Obsolete] [DropDownRef] public VarFloat _monoVarFloat;

        public IFloatProvider _monoVarFloatProvider;
        [DropDownRef]
        public VarFloat _monoVarFloat;
        public Operator op;
        public float targetValue;

        protected override bool IsValid
        {
            get
            {
                // if (_monoVarFloat == null)
                // {
                //     // Debug.LogError("variableFloat is null", this);
                //     return false;
                // }
                var value = _monoVarFloatProvider.Value;

                switch (op)
                {
                    case Operator.Equals:
                        return value == targetValue;
                    case Operator.NotEqual:
                        return value != targetValue;
                    case Operator.GreaterThan:
                        return value > targetValue;
                    case Operator.LessThan:
                        return value < targetValue;
                    case Operator.GreaterThanOrEqual:
                        return value >= targetValue;
                    case Operator.LessThanOrEqual:
                        return value <= targetValue;
                }

                return false;
            }
        }
    }
}