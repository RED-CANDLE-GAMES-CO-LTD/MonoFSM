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

        [Tooltip("啟用後從 Source 扣除等量值加到 Target（不足時只傳剩餘量）")]
        public bool _transfer;

        [ShowIf(nameof(_transfer))] [Required] public VarFloat _sourceVar;

        private string RateSign => _rateVar != null && _rateVar.Value >= 0 ? "+=" : "-=";

        private string RateDesc => _rateVar != null
            ? Mathf.Abs(_rateVar.Value).ToString(CultureInfo.InvariantCulture)
            : "null";

        [PreviewInInspector]
        public override string Description =>
            _transfer
                ? $"{(_sourceVar != null ? _sourceVar.Description : "null")} --({RateDesc}/s)--> {(_targetVar != null ? _targetVar.Description : "null")}"
                : $"{(_targetVar != null ? _targetVar.Description : "null")} {RateSign} {RateDesc} * dt";

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

                float desired = Mathf.Abs(_rateVar.Value) * DeltaTime;
                float available = Mathf.Max(0f, _sourceVar.CurrentValue - _sourceVar.Min);
                float actual = Mathf.Min(desired, available);
                if (actual <= 0f) return;

                _sourceVar.SetValue(_sourceVar.CurrentValue - actual, this);
                _targetVar.SetValue(_targetVar.CurrentValue + actual, this);
                return;
            }

            _targetVar.SetValue(_targetVar.Value + _rateVar.Value * DeltaTime, this);
        }
    }
}
