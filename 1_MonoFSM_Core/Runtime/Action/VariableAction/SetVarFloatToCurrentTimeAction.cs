using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    /// <summary>
    /// 把當下的模擬時間（WorldUpdateSimulator.SimulationTime）記進目標 VarFloat，
    /// 之後配 SinceVarFloatTimeStampCondition 算時間差做冷卻，不需要 timer 倒數。
    /// </summary>
    public class SetVarFloatToCurrentTimeAction : AbstractStateAction
    {
        [Required] [DropDownRef] [SerializeField]
        private VarFloat _targetVar;

        public override string Description => "Stamp $" + (_targetVar != null ? _targetVar.name : "?") + " = now";

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null)
            {
                Debug.LogError($"[SetVarFloatToCurrentTimeAction] Target variable is null in {name}", this);
                return;
            }

            _targetVar.SetValue(WorldUpdateSimulator.SimulationTime, this);
        }
    }
}
