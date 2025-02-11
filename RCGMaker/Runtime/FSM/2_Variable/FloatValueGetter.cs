using RCGFSM.Variable;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    //functional 來獲取float value (lazy evaluation)
    public class FloatValueGetter:MonoBehaviour, IFloatValueProvider
    {
        public FloatValueSource value1;
        public FloatValueSource value2;
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