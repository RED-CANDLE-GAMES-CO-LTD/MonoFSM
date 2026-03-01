using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Action.VariableAction
{
    /// <summary>
    /// target -= rate * deltaTime
    /// </summary>
    public class FloatConsumePerSecondAction : AbstractStateAction
    {
        [Required] public VarFloat _targetVar;

        public VarFloatWrapper _rateVar;

        [PreviewInInspector]
        public override string Description =>
            $"{(_targetVar != null ? _targetVar.Description : "null")} -= {(_rateVar != null ? _rateVar.Description : "null")} * dt";

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null || _rateVar == null)
            {
                Debug.LogError("FloatConsumePerSecondAction: Target or Rate variable is not set.",
                    this);
                return;
            }

            _targetVar.SetValue(_targetVar.Value - _rateVar.Value * DeltaTime, this);
        }
    }
}
