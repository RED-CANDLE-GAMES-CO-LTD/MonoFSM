using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    /// <summary>
    ///     _dealer 現在有沒有打到任何 receiver（讀 GeneralEffectDealer.HasReceiverOverlap，pull 式每次重算）。
    ///     值來自 dealer 的命中帳本，只有 EffectDetector 有 Simulate 時才會更新：
    ///     沒 state authority 的 client、或開場就被 simulation culling（還沒人靠近過）時帳本是空的，永遠 false；
    ///     靠近過再被 cull 則沿用凍結的值。要跨機器 / 不受 culling 影響的狀態，改在 enter/exit 寫一顆 networked VarBool。
    /// </summary>
    public class IsDealerHitAnyReceiverCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        [SerializeField]
        private GeneralEffectDealer _dealer;

        protected override bool IsValid => _dealer?.HasReceiverOverlap ?? false;

        public override string Description =>
            $"Dealer ${_dealer?.Description} hit any?";

        //FIXME: 要檢查gameObject是不是關的？但有可能動畫控制？hmmmm註解和動畫控制分不清楚
    }
}

