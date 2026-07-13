using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver.ApplyEffect
{
    // //最完整的應該用這個
    public class FloatMathAction : AbstractStateAction
    {
        [Required]
        public VarFloat _targetVar;

        [SerializeField]
        private VarFloatWrapper _source1Var = new();

        [ShowIf(nameof(IsSource2Needed))]
        [SerializeField]
        private VarFloatWrapper _source2Var = new();

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
            return Arithmetic != ArithmeticType.AdditionAssign
                && Arithmetic != ArithmeticType.SubtractionAssign
                && Arithmetic != ArithmeticType.Transfer;
        }

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
                var source1Desc = _source1Var.Description + srcMod;

                return Arithmetic switch
                {
                    ArithmeticType.AdditionAssign => $"{targetDesc} += {source1Desc}",
                    ArithmeticType.SubtractionAssign => $"{targetDesc} -= {source1Desc}",
                    ArithmeticType.Transfer => $"{source1Desc} --({_transferAmount?.Value})--> {targetDesc}",
                    _ =>
                        $"{targetDesc} = {source1Desc} {ArithmeticString} {_source2Var.Description}{srcMod}",
                };
            }
        }

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
                float min = _source1Var._var != null ? _source1Var._var.Min : 0f;
                float available = Mathf.Max(0f, _source1Var.Value - min);
                float actualTransfer = Mathf.Min(_transferAmount.Value, available);
                if (actualTransfer <= 0f) return;

                _source1Var.SetValue(_source1Var.Value - actualTransfer, this);
                _targetVar.SetValue(_targetVar.CurrentValue + actualTransfer, this);
                return;
            }

            var value1 = _source1Var.Value * _sourceModifier.Value;
            float result;
            if (Arithmetic == ArithmeticType.AdditionAssign)
            {
                _targetVar.AddBy(value1, this); //直接用AddBy，避免modifier被套用兩次
            }
            else if (Arithmetic == ArithmeticType.SubtractionAssign)
            {
                _targetVar.AddBy(-value1, this);
            }
            else
            {
                var value2 = _source2Var.Value * _sourceModifier.Value;
                result = Calculate(value1, value2);
                _targetVar.SetValue(result, this);
            }
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
