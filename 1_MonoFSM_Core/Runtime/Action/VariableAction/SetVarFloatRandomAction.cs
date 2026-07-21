using _1_MonoFSM_Core.Runtime.Utilities;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    /// <summary>
    /// 執行時抽一個 [_min, _max] 範圍內的隨機值寫進 _targetVar。
    /// 掛在 State 的 Enter 上就是「進 State 當下抽一次、抽起來放」，整段 State 期間門檻固定。
    /// 聯網 deterministic：用固定 _seed 搭配 WorldUpdateSimulator.CurrentTick（見 TickRandom）。
    /// 需要 per-instance 各異時，掛一個回傳身份 id 的 IIntProvider 子物件到 _seedSource（見下方註解）。
    /// </summary>
    public class SetVarFloatRandomAction : AbstractStateAction
    {
        [DropDownRef] [SerializeField] private VarFloat _targetVar;

        [SerializeField] private float _min;
        [SerializeField] private float _max = 1f;

        [Tooltip("固定 salt，聯網時每台機器要一致")] [SerializeField]
        private int _seed = 12345;

        // 選配：回傳「實例身份 id」的 provider（掛成子物件自動抓）。
        // 有掛時 seed = Combine(_seed, id)，讓同 tick 的不同實例抽到不同值。
        // 場景物件用 BakedInstanceSeedProvider，網路 spawn 用 Fusion 那邊的 NetworkObjectId provider，
        // TickRandom / 本 Action 都不需認識網路。
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private IIntProvider _seedSource;

        private int Seed => _seedSource != null ? TickRandom.Combine(_seed, _seedSource.IntValue) : _seed;

        public override string Description =>
            $"Set {_targetVar?.Description} = Random[{_min}, {_max}] seed={_seed}";

        protected override void OnActionExecuteImplement()
        {
            var value = TickRandom.Range(Seed, WorldUpdateSimulator.CurrentTick, _min, _max);
            _targetVar.SetValue(value, this);
        }
    }
}
