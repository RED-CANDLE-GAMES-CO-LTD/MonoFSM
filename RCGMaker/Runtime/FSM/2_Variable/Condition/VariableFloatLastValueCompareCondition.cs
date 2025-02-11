using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VariableFloatLastValueCompareCondition : AbstractConditionComp
    {
        //講中文
        [FormerlySerializedAs("variableFloat")] [InfoBox("比較VariableFloat的LastValue和CalValue")] [DropDownRef]
        public MonoVariableFloat _monoVariableFloat;

        public Operator op;

        protected override string nameDescription => _monoVariableFloat
            ? name = "[Condition] " + _monoVariableFloat.name + " LastValue " + op + " CurrentValue"
            : "[Condition] VariableFloatLastValueCompareCondition";

        protected override bool isValid
        {
            get
            {
                if (_monoVariableFloat == null)
                {
                    return false;
                }

//15 -> 10, Last 15, Current 10,
                // this.Log("LastValue Compare: " , variableFloat.LastValue , " CalValue: " ,variableFloat.CurrentValue);
                return op switch
                {
                    Operator.Equals => _monoVariableFloat.CurrentValue == _monoVariableFloat.LastValue,
                    Operator.NotEqual => _monoVariableFloat.CurrentValue != _monoVariableFloat.LastValue,
                    Operator.GreaterThan => _monoVariableFloat.CurrentValue > _monoVariableFloat.LastValue,
                    Operator.LessThan => _monoVariableFloat.CurrentValue < _monoVariableFloat.LastValue,
                    Operator.GreaterThanOrEqual => _monoVariableFloat.CurrentValue >= _monoVariableFloat.LastValue,
                    Operator.LessThanOrEqual => _monoVariableFloat.CurrentValue <= _monoVariableFloat.LastValue,
                    _ => false
                };
            }
        }
    }
}