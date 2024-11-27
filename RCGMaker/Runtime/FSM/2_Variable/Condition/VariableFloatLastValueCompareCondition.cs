using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VariableFloatLastValueCompareCondition : AbstractConditionComp
    {
        //講中文
        [InfoBox("比較VariableFloat的LastValue和CalValue")]
        [DropDownRef] public VariableFloat variableFloat;
        public Operator op;

        protected override string nameDescription => name = "[Condition] " + variableFloat.name + " LastValue " + op +" CurrentValue";

        protected override bool isValid
        {
            get
            {
                if (variableFloat == null)
                {
                    return false;
                }
//15 -> 10, Last 15, Current 10,
                // this.Log("LastValue Compare: " , variableFloat.LastValue , " CalValue: " ,variableFloat.CurrentValue);
                return op switch
                {
                    Operator.Equals => variableFloat.CurrentValue == variableFloat.LastValue,
                    Operator.NotEqual => variableFloat.CurrentValue != variableFloat.LastValue,
                    Operator.GreaterThan => variableFloat.CurrentValue > variableFloat.LastValue,
                    Operator.LessThan => variableFloat.CurrentValue < variableFloat.LastValue,
                    Operator.GreaterThanOrEqual => variableFloat.CurrentValue >= variableFloat.LastValue,
                    Operator.LessThanOrEqual => variableFloat.CurrentValue <= variableFloat.LastValue,
                    _ => false
                };
            }
        }
    }
}