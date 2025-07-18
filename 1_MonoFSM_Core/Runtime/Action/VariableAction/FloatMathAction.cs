using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.DataProvider;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSM.VarRefOld;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver.ApplyEffect
{
    // public abstract class AbstractEffectHitAction : AbstractStateAction, IArgEventReceiver<GeneralEffectHitData>
    // {
    //     public void ArgEventReceived(GeneralEffectHitData arg)
    //     {
    //         _runtimeDealer = arg.Dealer as GeneralEffectDealer;
    //         _runtimeReceiver = arg.Receiver as GeneralEffectReceiver;
    //         
    //         // Debug.Log("EffectHitAction EventReceived", this);
    //         ApplyEffect(arg.Dealer as GeneralEffectDealer, arg.Receiver as GeneralEffectReceiver);
    //     }
    //
    //     protected abstract void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver);
    //     [AutoParent] [PreviewInInspector] private GeneralEffectDealer _runtimeDealer;
    //     [AutoParent] [PreviewInInspector] private GeneralEffectReceiver _runtimeReceiver;
    //
    //     public override void EventReceived<T>(T arg)
    //     {
    //         var data = arg as GeneralEffectHitData;
    //         if (data == null) return;
    //         ArgEventReceived(data);
    //     }
    //
    //     // public void EventReceived()
    //     // {
    //     //     //沒參數？這樣算有問題？
    //     //     throw new System.NotImplementedException();
    //     // }
    //
    //     // public bool IsValid 
    // }
    //
    // //最完整的應該用這個
    public class FloatMathAction : AbstractStateAction
    {
        //FIXME: value overrider?
        
        // public enum OperandType
        // {
        //     Dealer,
        //
        //     Receiver
        //     // Constant
        [AutoChildren] [CompRef] private TargetVarRef _targetVariableProvider; // }

        [AutoChildren] [CompRef] private SourceValueRef _source1VariableProvider;

        [ShowIf(nameof(IsSource2Needed))] [AutoChildren] [CompRef]
        private SourceValue2Ref _source2VariableProvider;



        public ArithmeticType Arithmetic;

        // private bool IsSource2NotNeeded()
        // {
        //     return Arithmetic == ArithmeticType.AdditionAssign || Arithmetic == ArithmeticType.SubtractionAssign;
        // }

        private bool IsSource2Needed()
        {
            return Arithmetic != ArithmeticType.AdditionAssign && Arithmetic != ArithmeticType.SubtractionAssign;
        }

        // public OperandType _setter;
        // public OperandType _operator1;
        //
        // public OperandType _operator2;

        // private VariableTag op1 => _operator1 == OperandType.Dealer ? dealerVariableProvider?._varTag : receiverVariableProvider?._varTag;

        // private VariableTag op2 =>
        //     _operator2 == OperandType.Dealer ? dealerVariableProvider?._varTag : receiverVariableProvider?._varTag;

        private AbstractMonoVariable setterVariable => _targetVariableProvider?.VarRaw;
        // _setter == OperandType.Dealer
        //     ? dealerVariableProvider?.GetVarRaw()
        //     : receiverVariableProvider?.GetVarRaw();

        private string ArithmeticString => Arithmetic switch
        {
            ArithmeticType.Add => "+",
            ArithmeticType.Subtract => "-",
            ArithmeticType.Multiply => "*",
            ArithmeticType.Divide => "/",
            ArithmeticType.Modulo => "%",
            _ => "+"
        };

        [PreviewInInspector]
        public override string Description
        {
            get
            {
                switch (Arithmetic)
                {
                    case ArithmeticType.AdditionAssign:
                        return $"{setterVariable?.name} += {_source1VariableProvider}";
                    case ArithmeticType.SubtractionAssign:
                        return $"{setterVariable?.name} -= {_source1VariableProvider}";
                    default:
                        return
                            $"{setterVariable?.name} = {_source1VariableProvider?.ToString()} {ArithmeticString} {_source2VariableProvider}";
                }
            }
        }
        // $"{setterVariable?.name} = {_operator1}.{op1?.name} {ArithmeticString} {_operator2}.{op2?.name}";
        //要用entry?


        // [DropDownRef] public VariableFloat dealerVariable;


        //FIXME: target Variable會交換...有時候想處理的是Dealer，有時候想處理的是Receiver

        public enum ArithmeticType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo,
            AdditionAssign,
            SubtractionAssign
            //+=,-=?
        }

        // protected override void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver)
        protected override void OnActionExecuteImplement()
        {
            if (_targetVariableProvider == null || _source1VariableProvider == null)
            {
                Debug.LogError("EffectHitFloatArithmeticAction: Target or Source1 variable provider is not set.", this);
                return;
            }

            var targetVar = _targetVariableProvider.GetVar<VarFloat>();
            if (targetVar == null)
            {
                Debug.LogError("EffectHitFloatArithmeticAction: Target variable is not a VarFloat.", this);
                return;
            }

            var targetValue = targetVar.Value;
            var value1 = _source1VariableProvider.GetValue<float>();
            float result = 0;
            if (Arithmetic == ArithmeticType.AdditionAssign)
                result = targetValue + value1;
            else if (Arithmetic == ArithmeticType.SubtractionAssign)
                result = targetValue - value1;
            else
            {
                var value2 = _source2VariableProvider.GetValue<float>();
                result = Calculate(value1, value2);
            }


            _targetVariableProvider.VarRaw.SetValue(result, this);
            // var dealerValue = dealerVariableProvider.GetValueFrom(dealer);
            // var receiverValue = receiverVariableProvider.GetValueFrom(receiver);
            // Debug.Log(
            //     $"{_setter} = {dealerVariableProvider._varTag.name} dealerValue: {dealerValue}, {Arithmetic} {receiverVariableProvider._varTag.name} receiverValue: {receiverValue}",
            //     this);
            // var value1 = _operator1 == OperandType.Dealer ? dealerValue : receiverValue;
            // var value2 = _operator2 == OperandType.Dealer ? dealerValue : receiverValue;
            // if (_setter == OperandType.Dealer)
            //     dealerVariableProvider.SetValue(
            //         Calculate(value1, value2), this);
            // else
            //     receiverVariableProvider.SetValue(
            //         Calculate(value1, value2), this);
        }

        private float Calculate(float source1, float source2)
        {
            return Arithmetic switch
            {
                ArithmeticType.Add => source1 + source2,
                ArithmeticType.Subtract => source1 - source2,
                ArithmeticType.Multiply => source1 * source2,
                ArithmeticType.Divide => source1 / source2,
                ArithmeticType.Modulo => source1 % source2,

                _ => source1
            };
        }
    }
}