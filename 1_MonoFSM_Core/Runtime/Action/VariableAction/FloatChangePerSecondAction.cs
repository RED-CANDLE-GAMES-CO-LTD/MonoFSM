using System.Globalization;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Action.VariableAction
{
    /// <summary>
    /// target += rate * deltaTime (rate > 0 增加, rate &lt; 0 消耗)
    /// Transfer 模式：從 source 扣除 rate * deltaTime，加到 target（不足時只傳剩餘量）
    /// </summary>
    public class FloatChangePerSecondAction : AbstractStateAction
    {
        [Required] public VarFloat _targetVar;

        public VarFloatWrapper _rateVar = new(1f);

        public float _multiplier = 1f;

        [Tooltip("啟用後從 Source 扣除等量值加到 Target（不足時只傳剩餘量）")]
        public bool _transfer;

        [ShowIf(nameof(_transfer))] [Required] public VarFloat _sourceVar;

        private float EffectiveRate => (_rateVar != null ? _rateVar.Value : 0f) * _multiplier;

        private string MultiplierSign => _multiplier >= 0 ? "+=" : "-=";

        private string MultiplierDesc =>
            Mathf.Abs(_multiplier).ToString(CultureInfo.InvariantCulture);

        private string RateVarDesc => _rateVar != null ? _rateVar.Description : "null";

        [PreviewInInspector]
        public override string Description =>
            _transfer
                ? $"{(_sourceVar != null ? _sourceVar.Description : "null")} --({RateVarDesc} x{MultiplierDesc}/s)--> {(_targetVar != null ? _targetVar.Description : "null")}"
                : $"{(_targetVar != null ? _targetVar.Description : "null")} {MultiplierSign} {RateVarDesc} x{MultiplierDesc} * dt";

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null || _rateVar == null)
            {
                Debug.LogError("FloatChangePerSecondAction: Target or Rate variable is not set.",
                    this);
                return;
            }

            if (_transfer)
            {
                if (_sourceVar == null)
                {
                    Debug.LogError(
                        "FloatChangePerSecondAction: Transfer mode requires Source variable.",
                        this);
                    return;
                }

                float desired = Mathf.Abs(EffectiveRate) * DeltaTime;
                float available = Mathf.Max(0f, _sourceVar.CurrentValue - _sourceVar.Min);
                float actual = Mathf.Min(desired, available);
                if (actual <= 0f) return;

                _sourceVar.SetValue(_sourceVar.CurrentValue - actual, this);
                _targetVar.SetValue(_targetVar.CurrentValue + actual, this);
                return;
            }

            _targetVar.SetValue(_targetVar.Value + EffectiveRate * DeltaTime, this);
        }
    }
}
