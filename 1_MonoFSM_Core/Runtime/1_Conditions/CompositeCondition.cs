using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions
{
    /// <summary>
    ///     把子節點的 condition 用 AND 或 OR 組起來的容器（_operationType 選）。
    ///     需要「A 或 B 成立」時掛這顆，把各條件當它的子節點；也可以當 Var 的 bool valueSource
    ///     （例如 d_is_raining = 強制下雨 OR 累積機率滿）。
    ///     只撈深度一層的子 condition，自己被 disable 或沒有子條件時一律視為成立。
    /// </summary>
    public class CompositeCondition : AbstractConditionBehaviour
    {
        public override bool IsDrawingValueInfo => true;
        public override string ValueInfo => IsValid.ToString();

        [Tooltip("選擇條件組合的邏輯操作：AND (所有條件都必須滿足) 或 OR (至少一個條件滿足)")]
        [LabelText("操作類型")]
        [GUIColor(nameof(GetOperationTypeColor))]
        public CompositeOperationType _operationType = CompositeOperationType.And;

        private Color GetOperationTypeColor() => _operationType switch
        {
            CompositeOperationType.And => new Color(0.6f, 1f, 0.6f),  // 綠色
            CompositeOperationType.Or => new Color(1f, 0.85f, 0.5f),  // 橘色
            _ => Color.white
        };

        // protected override bool HasError()
        // {
        //     return base.HasError() || (_conditions == null || _conditions.Length < 1);
        // }

        [AutoChildren(DepthOneOnly = true, _isSelfInclude = false)]
        [CompRef]
        [RequiredListLength(1, null)]
        //可以是warning嗎？
        private AbstractConditionBehaviour[] _conditions;

        protected override string DescriptionTag => "if " + _operationType.ToString().ToUpper();

        public override string Description => FormatName(name);

        protected override bool IsValid
        {
            get
            {
                if (isActiveAndEnabled == false) //沒有開起來不算
                    return true; //FIXME: 關著當作過！雖然好像很怪？
                if (_conditions == null || _conditions.Length == 0)
                    return true;

                switch (_operationType)
                {
                    case CompositeOperationType.And:
                        return _conditions.IsAllValid(this);

                    case CompositeOperationType.Or:
                        return _conditions.IsAnyValid(this);

                    default:
                        return _conditions.IsAllValid(this);
                }
            }
        }
    }
}
