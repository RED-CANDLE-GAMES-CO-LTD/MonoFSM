using _1_MonoFSM_Core.Runtime.MonoData;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// 判斷距離上次 ViewRoot unmount 是否已超過門檻秒數（冷卻用）。
    /// 時間來源為 ViewRoot.SecondsSinceUnmount（WorldUpdateSimulator tick 換算，本地不同步）。
    /// </summary>
    public class SinceViewRootUnmountCondition : AbstractConditionBehaviour
    {
        [Required] [SerializeField] private ViewRoot _viewRoot;

        // 門檻秒數，可綁 Var 或直接填常數
        [SerializeField] private VarFloatWrapper _seconds = new(1f);

        protected override bool IsValid =>
            _viewRoot != null && _viewRoot.SecondsSinceUnmount >= _seconds.Value;

        public override string Description =>
            $"Since Unmount >= {_seconds.Description}s";
    }
}
