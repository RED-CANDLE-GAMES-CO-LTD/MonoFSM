using MonoFSM.FSM;
using MonoFSM.Condition;
using MonoFSM.Variable.Condition;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{

    /// <summary>
    /// 「_targetState 是不是它自己那台 FSM 的當前狀態」。跨 FSM 也成立 —— 問的是 _targetState.Owner
    /// 的當前狀態，不是掛著這顆 condition 的那台。要「不在該狀態」就勾 FinalResultInverted。
    /// 常見用途：把 Render / Action 閘在某個狀態底下（AbstractRenderBehaviour 的 _conditionGroup
    /// 會自動收直接子節點的 condition，所以掛成子節點就會生效）。
    /// </summary>
    public class IsStateCondition : AbstractConditionBehaviour
    {
        [ConditionPreset("Is State", Category = "State", Priority = 100, ColorHex = "#FFB347")]
        private static void Preset_State(IsStateCondition c)
        {
        }
        [Required]
        [DropDownRef]
        [SerializeField]
        GeneralState _targetState;

        protected override bool IsValid =>
            _targetState != null && _targetState.Owner != null &&
            _targetState.Owner.IsCurrentState(_targetState);

        //_owner.FsmContext.currentStateType == _targetState;
        public override string Description => $"Is {_targetState?.name}";

        protected override bool HasError()
        {
            return base.HasError() && _targetState != null && _targetState.isActiveAndEnabled;
        }
    }
}
