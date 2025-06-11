using UnityEngine;
using UnityEngine.Serialization;

using Sirenix.OdinInspector;
using MonoFSM.Variable;

namespace MonoFSM.Variable
{
    //這個應該直接用abstract class?
    public class VariableFloatArithmeticOperation : MonoBehaviour, IVariableFloatOperation
    {
        [SerializeField] ArithmeticOperator Operator;

        [SerializeField] 
        [HideIf(nameof(_operandMonoVar))]
        float anotherValue;

        [FormerlySerializedAs("_operandMonoVariable")]
        [FormerlySerializedAs("OperandVariable")] 
        [SerializeField]
        VarFloat _operandMonoVar;

        private float OperandValue 
            => _operandMonoVar == null 
                ? anotherValue 
                : _operandMonoVar.CurrentValue;

        public float ApplyOperation(float value)
            => Operator switch
            {
                ArithmeticOperator.Add => value + OperandValue,
                ArithmeticOperator.Sub => value - OperandValue,
                ArithmeticOperator.Mul => value * OperandValue,
                ArithmeticOperator.Div => value / OperandValue,
                _ => value
            };
    }
}