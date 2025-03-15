using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VariableFloatLastValueCompareCondition : AbstractConditionComp
    {
        //講中文
        [FormerlySerializedAs("_monoVariableFloat")]
        [FormerlySerializedAs("variableFloat")]
        [InfoBox("比較VariableFloat的LastValue和CalValue")]
        [DropDownRef]
        public VarFloat _monoVarFloat;

        public Operator op;

        protected override string nameDescription => _monoVarFloat
            ? name = "[Condition] " + _monoVarFloat.name + " LastValue " + op + " CurrentValue"
            : "[Condition] VariableFloatLastValueCompareCondition";

        protected override bool IsValid
        {
            get
            {
                if (_monoVarFloat == null)
                {
                    return false;
                }

//15 -> 10, Last 15, Current 10,
                // this.Log("LastValue Compare: " , variableFloat.LastValue , " CalValue: " ,variableFloat.CurrentValue);
                return op switch
                {
                    Operator.Equals => _monoVarFloat.CurrentValue == _monoVarFloat.LastValue,
                    Operator.NotEqual => _monoVarFloat.CurrentValue != _monoVarFloat.LastValue,
                    Operator.GreaterThan => _monoVarFloat.CurrentValue > _monoVarFloat.LastValue,
                    Operator.LessThan => _monoVarFloat.CurrentValue < _monoVarFloat.LastValue,
                    Operator.GreaterThanOrEqual => _monoVarFloat.CurrentValue >= _monoVarFloat.LastValue,
                    Operator.LessThanOrEqual => _monoVarFloat.CurrentValue <= _monoVarFloat.LastValue,
                    _ => false
                };
            }
        }
    }
}