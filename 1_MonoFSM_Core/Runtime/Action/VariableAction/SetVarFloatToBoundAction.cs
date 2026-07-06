using System;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    public class SetVarFloatToBoundAction : AbstractStateAction
    {
        public override string Description => "Set $" + _targetVar?.name + " -> " +
                                              _boundType +
                                              (_boundType >= BoundType.SetToPercentage
                                                  ? $" {_percentage * 100f:0}%"
                                                  : "");

        public enum BoundType
        {
            Min,
            Max,
            SetToPercentage,
            IncreaseByPercentage,
            DecreaseByPercentage
        }

        [Required]
        [DropDownRef] public VarFloat _targetVar;
        public BoundType _boundType = BoundType.Max;

        [Range(0f, 1f)]
        public float _percentage;

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null)
            {
                Debug.LogError($"[SetVarFloatToBoundAction] Target variable is null in {name}",
                    this);
                return;
            }
            var range = _targetVar.Max - _targetVar.Min;

            switch (_boundType)
            {
                case BoundType.Min:
                    _targetVar.SetValue(_targetVar.Min, this);
                    break;
                case BoundType.Max:
                    _targetVar.SetValue(_targetVar.Max, this);
                    break;
                case BoundType.SetToPercentage:
                    _targetVar.SetValue(_targetVar.Min + range * _percentage, this);
                    break;
                case BoundType.IncreaseByPercentage:
                    _targetVar.SetValue(_targetVar.CurrentValue + range * _percentage, this);
                    break;
                case BoundType.DecreaseByPercentage:
                    _targetVar.SetValue(_targetVar.CurrentValue - range * _percentage, this);
                    break;
            }
        }
    }
}
