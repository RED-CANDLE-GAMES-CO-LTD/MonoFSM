using _1_MonoFSM_Core.Runtime.Utilities;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 機率條件：以 _probability（0~1）的機率成立，掛在 action 的 [If] 底下做「有機率才做」（例：開寶箱 50% 噴道具）。
    /// 走 TickRandom（seed + tick），同一 tick 內重複判斷結果一致，聯網各端也一致；同一個 tick 有多顆要各自獨立就給不同 _seed，
    /// 或掛一個 IIntProvider 子物件提供 instance 身份。_probability 沒接 Var 時用 _fallbackProbability。
    /// </summary>
    public class RandomChanceCondition : AbstractConditionBehaviour
    {
        [Tooltip("成立機率 (0~1)")]
        [DropDownRef]
        [SerializeField]
        private VarFloat _probability;

        [Tooltip("_probability 沒接 Var 時用的機率")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _fallbackProbability = 0.5f;

        [Tooltip("固定 salt，聯網時每台機器要一致")]
        [SerializeField]
        private int _seed = 13579;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private IIntProvider _seedSource;

        [ShowInInspector]
        [Sirenix.OdinInspector.ReadOnly]
        private float _lastRoll = -1f;

        private float Probability => _probability != null ? _probability.CurrentValue : _fallbackProbability;

        public override string Description =>
            "機率 " + (_probability != null ? _probability.name : _fallbackProbability.ToString("0.##"));

        protected override bool IsValid
        {
            get
            {
                var seed = _seedSource != null ? TickRandom.Combine(_seed, _seedSource.IntValue) : _seed;
                _lastRoll = TickRandom.Value01(seed, WorldUpdateSimulator.CurrentTick);
                return _lastRoll < Probability;
            }
        }
    }
}
