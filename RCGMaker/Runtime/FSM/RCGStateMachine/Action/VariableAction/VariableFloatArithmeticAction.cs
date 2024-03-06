using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;

namespace RCGFSM.Variable
{
    public enum ArithmeticOperator
    {
        Add,
        Sub,
        Mul,
        Div
    }

    public class VariableFloatArithmeticAction : AbstractStateAction, IRCGArgEventReceiver<IEffectHitData>
    {
        [SerializeField] private Component testIFloatValue;
        [SerializeField] private VariableFloat targetFlag;
        [SerializeField] private ArithmeticOperator Arithmetic;
        public float Value;

        public void EventReceived(IEffectHitData arg)
        {
            var value = arg.Dealer.FinalValue;
            UpdateValue(value);
        }

        private void UpdateValue(float value)
        {
            switch (Arithmetic)
            {
                case ArithmeticOperator.Add:
                    targetFlag.SetValue(targetFlag.Value + value, this);
                    break;
                case ArithmeticOperator.Sub:
                    targetFlag.SetValue(targetFlag.Value - value, this);
                    break;
                case ArithmeticOperator.Mul:
                    targetFlag.SetValue(targetFlag.Value * value, this);
                    break;
                case ArithmeticOperator.Div:
                    targetFlag.SetValue(targetFlag.Value / value, this);
                    break;
            }
        }

        protected override void OnStateEnterImplement()
        {
            UpdateValue(Value);
        }
    }
}