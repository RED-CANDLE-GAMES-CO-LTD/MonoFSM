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
    /// </summary>
    public class FloatChangePerSecondAction : AbstractStateAction
    {
        [Required] public VarFloat _targetVar;

        public VarFloatWrapper _rateVar;

        private string RateSign => _rateVar != null && _rateVar.Value >= 0 ? "+=" : "-=";

        private string RateDesc => _rateVar != null
            ? Mathf.Abs(_rateVar.Value).ToString(CultureInfo.InvariantCulture)
            : "null";

        [PreviewInInspector]
        public override string Description =>
            $"{(_targetVar != null ? _targetVar.Description : "null")} {RateSign} {RateDesc} * dt";

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null || _rateVar == null)
            {
                Debug.LogError("FloatChangePerSecondAction: Target or Rate variable is not set.",
                    this);
                return;
            }

            _targetVar.SetValue(_targetVar.Value + _rateVar.Value * DeltaTime, this);
        }
    }
}
