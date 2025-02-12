using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.Interact.EffectHit.Resolver.ApplyEffect
{
    public abstract class AbstractEffectHitAction : MonoBehaviour, IRCGArgEventReceiver<IEffectHitData>
    {
        // public abstract void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver);
        public void EventReceived(IEffectHitData arg)
        {
            _runtimeReceiver = arg.Receiver as GeneralEffectReceiver;
            Debug.Log("EffectHitAction EventReceived", this);
            ApplyEffect(arg.Dealer, arg.Receiver);
        }

        protected abstract void ApplyEffect(IEffectDealer dealer, IEffectReceiver receiver);
        [PreviewInInspector] GeneralEffectReceiver _runtimeReceiver;
    }

    public class EffectHitFloatArithmeticAction : AbstractEffectHitAction
    {
        public enum OperandType
        {
            Dealer,
            Receiver,
        }

        public OperandType _setter;
        public OperandType _operator1;
        public ArithmeticType Arithmetic;
        public OperandType _operator2;

        private VariableTag op1 =>
            _operator1 == OperandType.Dealer ? dealerVariableProvider?.varTag : receiverVariableProvider?.varTag;

        private VariableTag op2 =>
            _operator2 == OperandType.Dealer ? dealerVariableProvider?.varTag : receiverVariableProvider?.varTag;

        AbstractMonoVariable setterVariable =>
            _setter == OperandType.Dealer
                ? dealerVariableProvider?.GetMonoVariable
                : receiverVariableProvider?.GetMonoVariable;

        string ArithmeticString => Arithmetic switch
        {
            ArithmeticType.Add => "+",
            ArithmeticType.Subtract => "-",
            ArithmeticType.Multiply => "*",
            ArithmeticType.Divide => "/",
            ArithmeticType.Modulo => "%",
            _ => "+"
        };

        [PreviewInInspector]
        string _description => $"{setterVariable?.name} = {op1?.name} {ArithmeticString} {op2?.name}";
        //要用entry?


        // [DropDownRef] public VariableFloat dealerVariable;

        public VariableProvider<float> dealerVariableProvider;
        public VariableProvider<float> receiverVariableProvider;

        //FIXME: target Variable會交換...有時候想處理的是Dealer，有時候想處理的是Receiver

        public enum ArithmeticType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo,
        }

        protected override void ApplyEffect(IEffectDealer dealer, IEffectReceiver receiver)
        {
            var dealerValue = dealerVariableProvider.Value;
            var receiverValue = receiverVariableProvider.GetValueFrom(receiver as GeneralEffectReceiver);
            Debug.Log($"dealerValue: {dealerValue}, receiverValue: {receiverValue}", this);
            var value1 = _operator1 == OperandType.Dealer ? dealerValue : receiverValue;
            var value2 = _operator2 == OperandType.Dealer ? dealerValue : receiverValue;
            if (_setter == OperandType.Dealer)
                dealerVariableProvider.SetValue(
                    Calculate(value1, value2), this);
            else
                receiverVariableProvider.SetValue(
                    Calculate(value1, value2), this);
        }

        private float Calculate(float dealerVariableCurrentValue, float getValueFrom)
        {
            return Arithmetic switch
            {
                ArithmeticType.Add => dealerVariableCurrentValue + getValueFrom,
                ArithmeticType.Subtract => dealerVariableCurrentValue - getValueFrom,
                ArithmeticType.Multiply => dealerVariableCurrentValue * getValueFrom,
                ArithmeticType.Divide => dealerVariableCurrentValue / getValueFrom,
                ArithmeticType.Modulo => dealerVariableCurrentValue % getValueFrom,
                _ => dealerVariableCurrentValue
            };
        }
    }
}