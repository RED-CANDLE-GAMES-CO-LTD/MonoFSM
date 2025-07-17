using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    public enum ArithmeticOperator
    {
        Add,
        Sub,
        Mul,
        Div
    }

    [Obsolete] //EffectHitFloatArithmeticAction
    public class VarFloatArithmeticAction : AbstractStateAction, IArgEventReceiver<IEffectHitData>
    {
        //兩種情境，一種是從dealer來，一種是固定值觸發
        public override string Description => 
            $"{targetFlag?._varTag?.name} {arithmeticSymbol}= {sourceType} {sourceType switch { ValueSourceType.Constant => ConstValue, _ => 0 }}";

        private string arithmeticSymbol => Arithmetic switch
        {
            ArithmeticOperator.Add => "+",
            ArithmeticOperator.Sub => "-",
            ArithmeticOperator.Mul => "*",
            ArithmeticOperator.Div => "/",
            _ => "+"
        };

        [DropDownRef] [SerializeField] private VarFloat targetFlag;
        [SerializeField] private ArithmeticOperator Arithmetic;

        //要直接用值？
        //上面會有EffectDealer or EffectReceiver?
        [ShowIf(nameof(sourceType), ValueSourceType.Constant)]
        public float ConstValue; //FIXME: 另外包？

        public enum ValueSourceType //FIXME: 太限定了
        {
            Dealer,
            Receiver,
            Constant,
            Variable,
            Provider
        }

        [FormerlySerializedAs("valueSource")] public ValueSourceType sourceType;

        //FIXME: 
        // public override void EventReceived<T>(T arg)
        // {
        //     //怎麼抽象化？ sourceValueProvider
        //     if (arg is Collision collision) DoOperation(collision.impulse.magnitude);
        // }

        [Obsolete]
        public void ArgEventReceived(IEffectHitData arg) //FIXME: runtime value source? 狀態接著？
        {
            switch (sourceType)
            {
                //這個感覺太細了
                case ValueSourceType.Dealer:
                    throw new System.NotImplementedException();
                    //     DoOperation(arg.Dealer.FinalValue);
                    break;
                case ValueSourceType.Receiver:
                    throw new System.NotImplementedException();
                    //     DoOperation(arg.Receiver.ReactValue);
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

            this.Log("VariableFloatArithmeticAction: ", targetFlag.CurrentValue);
        }

        //last value < current value


        protected override void OnActionExecuteImplement()
        {
            DoOperation(ConstValue);
        }
    }
}