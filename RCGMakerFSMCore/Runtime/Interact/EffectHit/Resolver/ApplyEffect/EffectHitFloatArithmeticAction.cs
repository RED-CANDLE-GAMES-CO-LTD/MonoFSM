using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.Interact.EffectHit.Resolver.ApplyEffect
{
    public abstract class AbstractEffectHitAction : MonoBehaviour, IRCGArgEventReceiver<IEffectHitData>
    {
        [Button]
        private void Rename()
        {
            name = "[EffectHitAction]" + GetType().Name;
        }

        // public abstract void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver);
        public void EventReceived(IEffectHitData arg)
        {
            _runtimeDealer = arg.Dealer as GeneralEffectDealer;
            _runtimeReceiver = arg.Receiver as GeneralEffectReceiver;
            // Debug.Log("EffectHitAction EventReceived", this);
            ApplyEffect(arg.Dealer as GeneralEffectDealer, arg.Receiver as GeneralEffectReceiver);
        }

        protected abstract void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver);
        [PreviewInInspector] GeneralEffectDealer _runtimeDealer;
        [PreviewInInspector] GeneralEffectReceiver _runtimeReceiver;
    }

    public class EffectHitFloatArithmeticAction : AbstractEffectHitAction
    {
        public enum OperandType
        {
            Dealer,
            Receiver,
            // Constant
        }

        public OperandType _setter;
        public OperandType _operator1;
        public ArithmeticType Arithmetic;
        public OperandType _operator2;

        private VariableTag op1 =>
            _operator1 == OperandType.Dealer ? dealerVariableProvider?._varTag : receiverVariableProvider?._varTag;

        private VariableTag op2 =>
            _operator2 == OperandType.Dealer ? dealerVariableProvider?._varTag : receiverVariableProvider?._varTag;

        AbstractMonoVariable setterVariable =>
            _setter == OperandType.Dealer
                ? dealerVariableProvider?.VarRaw
                : receiverVariableProvider?.VarRaw;

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
        string _description =>
            $"{setterVariable?.name} = {_operator1}.{op1?.name} {ArithmeticString} {_operator2}.{op2?.name}";
        //要用entry?


        // [DropDownRef] public VariableFloat dealerVariable;

        public VariableFloatProvider dealerVariableProvider;
        public VariableFloatProvider receiverVariableProvider;

        //FIXME: target Variable會交換...有時候想處理的是Dealer，有時候想處理的是Receiver

        public enum ArithmeticType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo,
        }

        protected override void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver)
        {
            var dealerValue = dealerVariableProvider.GetValueFrom(dealer);
            var receiverValue = receiverVariableProvider.GetValueFrom(receiver);
            Debug.Log(
                $"{_setter} = {dealerVariableProvider._varTag.name} dealerValue: {dealerValue}, {Arithmetic} {receiverVariableProvider._varTag.name} receiverValue: {receiverValue}",
                this);
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