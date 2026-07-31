using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 判斷距離「上次記錄的時間戳」是否已超過門檻秒數（冷卻用，不需要 timer 倒數）。
    /// 時間戳由 SetVarFloatToCurrentTimeAction 寫入，來源為 WorldUpdateSimulator.SimulationTime。
    /// 時間戳 <= 0 代表從沒觸發過，直接視為冷卻結束。
    /// </summary>
    public class SinceVarFloatTimeStampCondition : AbstractConditionBehaviour
    {
        [Required] [DropDownRef] [SerializeField]
        private VarFloat _timeStampVar;

        // 冷卻秒數，可綁 Var 或直接填常數
        [SerializeField] private VarFloatWrapper _seconds = new(1f);

        [ShowInDebugMode]
        private float SecondsSinceStamp
        {
            get
            {
                if (_timeStampVar == null) return float.PositiveInfinity;
                var stamp = _timeStampVar.CurrentValue;
                if (stamp <= 0f) return float.PositiveInfinity; //從沒記錄過
                return WorldUpdateSimulator.SimulationTime - stamp;
            }
        }

        protected override bool IsValid
        {
            get
            {
                if (_timeStampVar == null)
                {
                    Debug.LogError($"[SinceVarFloatTimeStampCondition] _timeStampVar is null in {name}", this);
                    return false;
                }

                return SecondsSinceStamp >= _seconds.Value;
            }
        }

        public override string Description =>
            $"Since {(_timeStampVar != null ? _timeStampVar.name : "?")} >= {_seconds.Description}s";
    }
}
