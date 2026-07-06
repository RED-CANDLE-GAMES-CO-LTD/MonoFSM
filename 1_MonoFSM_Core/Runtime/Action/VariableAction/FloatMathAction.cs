using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver.ApplyEffect
{
    // //最完整的應該用這個
    //FIXME: 想要 wrapper?
    public class FloatMathAction : AbstractStateAction
    {
        [Required]
        public VarFloat _targetVar;

        [Required]
        public VarFloat _source1Var;

        [ShowIf(nameof(IsSource2Needed))]
        public VarFloat _source2Var;

        [ShowIf(nameof(IsTransfer))]
        [Tooltip("每次傳輸的固定量（不足時只傳剩餘量）")]
        public VarFloatWrapper _transferAmount;

        [FoldoutGroup("倍率調整")]
        [HideIf(nameof(IsTransfer))]
        [Tooltip("套用到 source 讀值的倍率（source1、source2 都會乘）")]
        public VarFloatWrapper _sourceModifier = new(1f);

        [FoldoutGroup("倍率調整")]
        [HideIf(nameof(IsTransfer))]
        [Tooltip("套用到 target 讀值的倍率（Assign 系列計算前先乘）")]
        public VarFloatWrapper _targetModifier = new(1f);

        public ArithmeticType Arithmetic = ArithmeticType.AdditionAssign; //default 最常用

        private bool IsTransfer => Arithmetic == ArithmeticType.Transfer;

        private bool IsSource2Needed()
        {
            if (_source1Var == null)
                return false;
            return Arithmetic != ArithmeticType.AdditionAssign
                && Arithmetic != ArithmeticType.SubtractionAssign
                && Arithmetic != ArithmeticType.Transfer;
        }

        // public OperandType _setter;
        // public OperandType _operator1;
        //
        // public OperandType _operator2;

        // private VariableTag op1 => _operator1 == OperandType.Dealer ? dealerVariableProvider?._varTag : receiverVariableProvider?._varTag;

        // private VariableTag op2 =>
        //     _operator2 == OperandType.Dealer ? dealerVariableProvider?._varTag : receiverVariableProvider?._varTag;

        // private AbstractMonoVariable setterVariable => _targetVar?.VarRaw;
        // _setter == OperandType.Dealer
        //     ? dealerVariableProvider?.GetVarRaw()
        //     : receiverVariableProvider?.GetVarRaw();

        private string ArithmeticString =>
            Arithmetic switch
            {
                ArithmeticType.Add => "+",
                ArithmeticType.Subtract => "-",
                ArithmeticType.Multiply => "*",
                ArithmeticType.Divide => "/",
                ArithmeticType.Modulo => "%",
                _ => "+",
            };

        [PreviewInInspector]
        public override string Description
        {
            get
            {
                var srcMod = !Mathf.Approximately(_sourceModifier.Value, 1f) ? $"*{_sourceModifier.Value}" : "";
                var tgtMod = !Mathf.Approximately(_targetModifier.Value, 1f) ? $"*{_targetModifier.Value}" : "";
                var targetDesc = (_targetVar != null ? _targetVar.Description : "null") + tgtMod;
                var source1Desc = (_source1Var != null ? _source1Var.Description : "null") + srcMod;

                return Arithmetic switch
                {
                    ArithmeticType.AdditionAssign => $"{targetDesc} += {source1Desc}",
                    ArithmeticType.SubtractionAssign => $"{targetDesc} -= {source1Desc}",
                    ArithmeticType.Transfer => $"{source1Desc} --({_transferAmount?.Value})--> {targetDesc}",
                    _ =>
                        $"{targetDesc} = {source1Desc} {ArithmeticString} {_source2Var?.Description}{srcMod}",
                };
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
            SubtractionAssign,
            Transfer,
        }

        // protected override void ApplyEffect(GeneralEffectDealer dealer, GeneralEffectReceiver receiver)
        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null || _source1Var == null)
            {
                Debug.LogError(
                    "EffectHitFloatArithmeticAction: Target or Source1 variable provider is not set.",
                    this
                );
                return;
            }

            if (Arithmetic == ArithmeticType.Transfer)
            {
                float available = Mathf.Max(0f, _source1Var.CurrentValue - _source1Var.Min);
                float actualTransfer = Mathf.Min(_transferAmount.Value, available);
                if (actualTransfer <= 0f) return;

                _source1Var.SetValue(_source1Var.CurrentValue - actualTransfer, this);
                _targetVar.SetValue(_targetVar.CurrentValue + actualTransfer, this);
                return;
            }

            var targetValue = _targetVar.Value * _targetModifier.Value;
            var value1 = _source1Var.Value * _sourceModifier.Value;
            float result;
            if (Arithmetic == ArithmeticType.AdditionAssign)
            {
                // result = targetValue + value1;
                _targetVar.AddBy(value1, this); //直接用AddBy，避免modifier被套用兩次
            }

            else if (Arithmetic == ArithmeticType.SubtractionAssign)
            {
                _targetVar.AddBy(-value1, this);
            }
            // result = targetValue - value1;
            else
            {
                var value2 = _source2Var.Value * _sourceModifier.Value;
                result = Calculate(value1, value2);
                _targetVar.SetValue(result, this);
            }


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

                _ => source1,
            };
        }
    }
}
