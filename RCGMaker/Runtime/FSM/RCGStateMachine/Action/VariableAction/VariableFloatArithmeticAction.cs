using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        //要直接用值？
        //上面會有EffectDealer or EffectReceiver?
        [ShowIf(nameof(sourceType), ValueSourceType.Constant)]
        public float ConstValue;
        
        public enum ValueSourceType
        {
            Dealer,
            Receiver,
            Constant
        }
        [FormerlySerializedAs("valueSource")] public ValueSourceType sourceType;
        public void EventReceived(IEffectHitData arg) //FIXME: runtime value source? 狀態接著？
        {
            switch (sourceType)
            {
                case ValueSourceType.Dealer:
                    DoOperation(arg.Dealer.FinalValue);
                    break;
                case ValueSourceType.Receiver:
                    DoOperation(arg.Receiver.ReactValue);
                    break;
                case ValueSourceType.Constant:
                    DoOperation(ConstValue);
                    break;
                default:
                    DoOperation(ConstValue);
                    break;
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
            DoOperation(ConstValue);
        }
    }
}