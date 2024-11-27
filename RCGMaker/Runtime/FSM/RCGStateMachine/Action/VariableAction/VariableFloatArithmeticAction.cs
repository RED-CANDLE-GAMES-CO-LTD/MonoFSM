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
        // [SerializeField] private Component testIFloatValue;
        
        [DropDownRef]
        [SerializeField] private VariableFloat targetFlag;
        [SerializeField] private ArithmeticOperator Arithmetic;
        public float Value;

        public void EventReceived(IEffectHitData arg) //FIXME: runtime value source? 狀態接著？
        {
            var value = arg.Dealer.FinalValue;
            UpdateValue(value);
        }

        private void UpdateValue(float value)
        {
            switch (Arithmetic)
            {
                case ArithmeticOperator.Add:
                    targetFlag.SetValue(targetFlag.CurrentValue + value, this);
                    break;
                case ArithmeticOperator.Sub:
                    targetFlag.SetValue(targetFlag.CurrentValue - value, this);
                    break;
                case ArithmeticOperator.Mul:
                    targetFlag.SetValue(targetFlag.CurrentValue * value, this);
                    break;
                case ArithmeticOperator.Div:
                    targetFlag.SetValue(targetFlag.CurrentValue / value, this);
                    break;
            }

            Debug.Log("VariableFloatArithmeticAction: " + targetFlag.CurrentValue);
        }
        
        //last value < current value
        

        protected override void OnStateEnterImplement()
        {
            UpdateValue(Value);
        }
    }
}