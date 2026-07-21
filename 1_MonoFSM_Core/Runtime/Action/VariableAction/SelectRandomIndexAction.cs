using _1_MonoFSM_Core.Runtime.Utilities;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using MonoFSM.Core.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    /// <summary>
    /// 執行時從 _targetList 的長度隨機挑一個 index，設成該 VarList 的 CurrentIndex。
    /// 之後靠 CurrentListItem / CurrentIndex 就能拿到被選中的項目。
    /// 聯網 deterministic：用固定 _seed 搭配 WorldUpdateSimulator.CurrentTick（見 TickRandom）。
    /// 需要 per-instance 各異時，掛一個回傳身份 id 的 IIntProvider 子物件到 _seedSource。
    /// </summary>
    public class SelectRandomIndexAction : AbstractStateAction
    {
        [DropDownRef] [SerializeField] private AbstractVarList _targetList;

        [Tooltip("固定 salt，聯網時每台機器要一致")] [SerializeField]
        private int _seed = 12345;

        // 選配：回傳「實例身份 id」的 provider（掛成子物件自動抓）。
        // 有掛時 seed = Combine(_seed, id)，讓同 tick 的不同實例抽到不同 index。
        [CompRef] [AutoChildren(DepthOneOnly = true)]
        private IIntProvider _seedSource;

        private int Seed => _seedSource != null ? TickRandom.Combine(_seed, _seedSource.IntValue) : _seed;

        public override string Description =>
            $"Select Random Index of {(_targetList != null ? _targetList.name : "?")} (seed={_seed})";

        protected override void OnActionExecuteImplement()
        {
            var count = _targetList.Count;
            if (count <= 0)
            {
                Debug.LogWarning("SelectRandomIndexAction: list is empty, skip.", this);
                return;
            }

            var index = TickRandom.RangeInt(Seed, WorldUpdateSimulator.CurrentTick, 0, count);
            _targetList.SetCurrentIndexTo(index);
        }
    }
}
