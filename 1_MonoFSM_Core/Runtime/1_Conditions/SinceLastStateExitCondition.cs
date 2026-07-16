using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 判斷距離上次離開指定 state 是否已超過門檻秒數（冷卻用）。
    /// 時間來源為 GeneralState.SecondsSinceLastExit（WorldUpdateSimulator tick 換算，本地不同步）。
    /// </summary>
    public class SinceLastStateExitCondition : AbstractConditionBehaviour
    {
        [Required] [DropDownRef] [SerializeField]
        private GeneralState _targetState;

        // 門檻秒數，可綁 Var 或直接填常數
        [SerializeField] private VarFloatWrapper _seconds = new(1f);

        protected override bool IsValid =>
            _targetState != null && _targetState.SecondsSinceLastExit >= _seconds.Value;

        public override string Description =>
            $"Since {(_targetState != null ? _targetState.name : "?")} Exit >= {_seconds.Description}s";
    }
}
