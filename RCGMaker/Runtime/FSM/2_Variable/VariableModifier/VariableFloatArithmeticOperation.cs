using RCGFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VariableFloatArithmeticOperation : MonoBehaviour, IVariableFloatOperation
    {
        [SerializeField] ArithmeticOperator Operator;

        [SerializeField] [HideIf(nameof(OperandVariable))]
        float anotherValue;

        [SerializeField] VariableFloat OperandVariable;

        private float OperandValue => OperandVariable == null ? anotherValue : OperandVariable.Value;

        public float ApplyOperation(float value)
        {
            return Operator switch
            {
                ArithmeticOperator.Add => value + OperandValue,
                ArithmeticOperator.Sub => value - OperandValue,
                ArithmeticOperator.Mul => value * OperandValue,
                ArithmeticOperator.Div => value / OperandValue,
                _ => value
            };
        }
    }
}