using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 判斷距離上次 effect exit 是否已超過門檻秒數（冷卻用）。
    /// 時間來源為 EffectResolver.SecondsSinceLastExit（WorldUpdateSimulator tick 換算，本地不同步）。
    /// </summary>
    public class SinceLastEffectExitCondition : AbstractConditionBehaviour
    {
        [AutoParent] [Required] [SerializeField]
        private EffectResolver _resolver;

        // 門檻秒數，可綁 Var 或直接填常數
        [SerializeField] private VarFloatWrapper _seconds = new(1f);

        protected override bool IsValid =>
            _resolver != null && _resolver.SecondsSinceLastExit >= _seconds.Value;

        public override string Description =>
            $"Since Exit >= {_seconds.Description}s";
    }
}
