using RCGFSM.Variable;
using UnityEngine;

namespace MonoFSM.Variable
{
    //functional 來獲取float value (lazy evaluation)
    public class FloatValueGetter : MonoBehaviour, IFloatValueProvider
    {
        public IFloatValueProvider value1;
        public IFloatValueProvider value2;
        public ArithmeticOperator op;

        public float FinalValue => op switch
        {
            ArithmeticOperator.Add => value1.FinalValue + value2.FinalValue,
            ArithmeticOperator.Sub => value1.FinalValue - value2.FinalValue,
            ArithmeticOperator.Mul => value1.FinalValue * value2.FinalValue,
            ArithmeticOperator.Div => value1.FinalValue / value2.FinalValue,
            _ => 0
        };
    }
}