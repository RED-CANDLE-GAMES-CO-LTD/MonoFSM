using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Resolver.ApplyEffect
{
    /// <summary>
    /// 整數版本的數學運算 Action。所有來源/目標都使用 VarIntWrapper，
    /// 可接受 VarInt 引用或固定值，比直接綁定 VarInt 更通用。
    /// </summary>
    public class IntMathAction : AbstractStateAction
    {
        public VarIntWrapper _targetVar;

        public VarIntWrapper _source1Var;

        [ShowIf(nameof(IsSource2Needed))]
        public VarIntWrapper _source2Var;

        [ShowIf(nameof(IsTransfer))]
        [Tooltip("每次傳輸的固定量（不足時只傳剩餘量）")]
        public VarIntWrapper _transferAmount;

        public ArithmeticType Arithmetic;

        private bool IsTransfer => Arithmetic == ArithmeticType.Transfer;

        private bool IsSource2Needed()
        {
            if (_source1Var == null)
                return false;
            return Arithmetic != ArithmeticType.AdditionAssign
                && Arithmetic != ArithmeticType.SubtractionAssign
                && Arithmetic != ArithmeticType.CycleAdditionAssign
                && Arithmetic != ArithmeticType.CycleSubtractionAssign
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
                var targetDesc = _targetVar != null ? _targetVar.Description : "null";
                var source1Desc = _source1Var != null ? _source1Var.Description : "null";

                return Arithmetic switch
                {
                    ArithmeticType.AdditionAssign => $"{targetDesc} += {source1Desc}",
                    ArithmeticType.SubtractionAssign => $"{targetDesc} -= {source1Desc}",
                    ArithmeticType.CycleAdditionAssign => $"{targetDesc} += {source1Desc} (cycle [Min, Max))",
                    ArithmeticType.CycleSubtractionAssign => $"{targetDesc} -= {source1Desc} (cycle [Min, Max))",
                    ArithmeticType.Transfer => $"{source1Desc} --({_transferAmount?.Value})--> {targetDesc}",
                    _ =>
                        $"{targetDesc} = {source1Desc} {ArithmeticString} {_source2Var?.Description}",
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
            CycleAdditionAssign,
            CycleSubtractionAssign,
        }

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null || _source1Var == null)
            {
                Debug.LogError(
                    "IntMathAction: Target or Source1 variable wrapper is not set.",
                    this
                );
                return;
            }

            if (Arithmetic == ArithmeticType.Transfer)
            {
                // 預設 source 的下限為 0（避免轉出負量）；若有 BoundModifier，SetValue 仍會走 clamp
                int available = Mathf.Max(0, _source1Var.Value);
                int actualTransfer = Mathf.Min(_transferAmount.Value, available);
                if (actualTransfer <= 0) return;

                _source1Var.SetValue(_source1Var.Value - actualTransfer, this);
                _targetVar.SetValue(_targetVar.Value + actualTransfer, this);
                return;
            }

            var targetValue = _targetVar.Value;
            var value1 = _source1Var.Value;
            int result;
            if (Arithmetic == ArithmeticType.AdditionAssign)
                result = targetValue + value1;
            else if (Arithmetic == ArithmeticType.SubtractionAssign)
                result = targetValue - value1;
            else if (Arithmetic == ArithmeticType.CycleAdditionAssign
                     || Arithmetic == ArithmeticType.CycleSubtractionAssign)
            {
                result = ComputeCycle(targetValue, value1,
                    Arithmetic == ArithmeticType.CycleAdditionAssign);
            }
            else
            {
                var value2 = _source2Var.Value;
                result = Calculate(value1, value2);
            }

            _targetVar.SetValue(result, this);
        }

        /// <summary>
        /// 在 [Min, Max) 半開區間內循環。Min/Max 取自 target VarInt 的 BoundModifier，
        /// 若沒掛 BoundModifier（或 range 非正），退化成普通 += / -=。
        /// </summary>
        private int ComputeCycle(int targetValue, int delta, bool isAdd)
        {
            int min = 0;
            int max = int.MaxValue;
            if (_targetVar._var != null)
            {
                min = _targetVar._var.Min;
                max = _targetVar._var.Max;
            }

            int range = max - min;
            int signedDelta = isAdd ? delta : -delta;
            if (range <= 0)
            {
                Debug.LogWarning(
                    $"IntMathAction Cycle: target VarInt has no valid [Min, Max) range (min={min}, max={max}); falling back to plain assign.",
                    this);
                return targetValue + signedDelta;
            }

            int shifted = targetValue + signedDelta - min;
            int mod = ((shifted % range) + range) % range;
            return mod + min;
        }

        private int Calculate(int source1, int source2)
        {
            return Arithmetic switch
            {
                ArithmeticType.Add => source1 + source2,
                ArithmeticType.Subtract => source1 - source2,
                ArithmeticType.Multiply => source1 * source2,
                ArithmeticType.Divide => source2 != 0 ? source1 / source2 : source1,
                ArithmeticType.Modulo => source2 != 0 ? source1 % source2 : source1,
                _ => source1,
            };
        }
    }
}
