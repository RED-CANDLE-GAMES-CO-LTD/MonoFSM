using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper.Float
{
    /// <summary>
    /// 距離「上次記錄的時間戳」的冷卻進度，0 = 剛蓋上時間戳、1 = 冷卻完成（clamp 到 0~1）。
    /// 時間戳由 SetVarFloatToCurrentTimeAction 寫入，冷卻是否結束用 SinceVarFloatTimeStampCondition 判斷，
    /// 這支只負責給視覺（量規填充、燈號、UI 倒數）用的 0~1 進度，三者要指向同一顆時間戳 Var 與同一組秒數。
    /// 時間戳 &lt;= 0 代表從沒觸發過，視為冷卻已完成回 1。
    /// </summary>
    public class TimeStampProgressValueSource : AbstractValueSource<float>, IFloatProvider
    {
        [Required] [DropDownRef] [SerializeField]
        private VarFloat _timeStampVar;

        [Tooltip("冷卻秒數，要跟對應的 SinceVarFloatTimeStampCondition 填一樣的值")]
        [SerializeField] private VarFloatWrapper _seconds = new(1f);

        [Tooltip("記關卡時間（LevelSimulationTime，關卡開始為 0）而不是全域 SimulationTime。" +
                 "要跟寫入端的 SetVarFloatToCurrentTimeAction 勾一樣，時間基準才一致")]
        [SerializeField] private bool _useLevelSimulationTime;

        private float Now => _useLevelSimulationTime
            ? WorldUpdateSimulator.LevelSimulationTime
            : WorldUpdateSimulator.SimulationTime;

        public override float Value
        {
            get
            {
                if (_timeStampVar == null)
                {
                    Debug.LogError($"[TimeStampProgressValueSource] _timeStampVar is null in {name}", this);
                    return 1f;
                }

                var stamp = _timeStampVar.CurrentValue;
                if (stamp <= 0f) return 1f; //從沒記錄過，視為冷卻已完成

                var seconds = _seconds.Value;
                if (seconds <= 0f) return 1f;

                return Mathf.Clamp01((Now - stamp) / seconds);
            }
        }

        public override string Description =>
            $"Progress since {(_timeStampVar != null ? _timeStampVar.name : "?")} / {_seconds.Description}s";
    }
}
