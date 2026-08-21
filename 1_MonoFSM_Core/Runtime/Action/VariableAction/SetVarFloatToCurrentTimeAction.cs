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

        [Tooltip("記關卡時間（LevelSimulationTime，關卡開始為 0）而不是全域 SimulationTime。" +
                 "要跟 FloatLevelSimulationTime 做時間差的話一定要勾，時間基準才一致")]
        [SerializeField]
        private bool _useLevelSimulationTime;

        public override string Description => "Stamp $" + (_targetVar != null ? _targetVar.name : "?") +
                                             (_useLevelSimulationTime ? " = level now" : " = now");

        protected override void OnActionExecuteImplement()
        {
            if (_targetVar == null)
            {
                Debug.LogError($"[SetVarFloatToCurrentTimeAction] Target variable is null in {name}", this);
                return;
            }

            _targetVar.SetValue(
                _useLevelSimulationTime
                    ? WorldUpdateSimulator.LevelSimulationTime
                    : WorldUpdateSimulator.SimulationTime, this);
        }
    }
}
