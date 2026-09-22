using System;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    /// <summary>
    ///     把 _targetVar 這顆 VarFloat 一次設到它自己的上下界：Min / Max，
    ///     或依 _percentage 設成 Min + (Max-Min) * p、以及在當前值上增減 (Max-Min) * p。
    ///     「修好就補滿血」「耗盡就歸零」這種一次性歸位用這顆，不要用 SetVarFloatConstAction 寫死數字（Max 一改就失準）。
    ///     Min/Max 會沿 varRef 轉發，所以 _targetVar 指到跨 entity 的 proxy Var 也算得對。
    /// </summary>
    [QuickCreate]
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

        [OnValueChanged(nameof(Rename))]
        [Required]
        [DropDownRef] public VarFloat _targetVar;

        [OnValueChanged(nameof(Rename))]
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
            this.Log("SetVarFloatToBoundAction", _targetVar);
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
