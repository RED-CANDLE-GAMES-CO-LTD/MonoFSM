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
        //兩種情境，一種是從dealer來，一種是固定值觸發
        
        [DropDownRef]
        [SerializeField] private VariableFloat targetFlag;
        [SerializeField] private ArithmeticOperator Arithmetic;
        public float Value;
        public bool IsConstantValue;
        
        public void EventReceived(IEffectHitData arg) //FIXME: runtime value source? 狀態接著？
        {
            if(IsConstantValue)
                DoOperation(Value);
            else
            {
                DoOperation(arg.Dealer.FinalValue);
            }
        }

        private void DoOperation(float value)
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
            DoOperation(Value);
        }
    }
}