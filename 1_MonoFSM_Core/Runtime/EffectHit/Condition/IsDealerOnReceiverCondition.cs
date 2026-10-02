using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    /// <summary>
    /// 「現在有沒有任何 dealer 疊在這顆 _receiver 上」（讀 GeneralEffectReceiver.HasDealerOverlap，pull 式每次重算）。
    /// 只看 live overlap：dealer 有效（或被 culling 凍結）就算，不看 receiver 自己的 [If] 條件、也不分是誰插的。
    /// 跟 IsDealerHitAnyReceiverCondition 是同一件事的兩端：那顆站在 dealer 問「我有沒有打到人」，這顆站在 receiver 問「有沒有人打到我」。
    /// 例：PPlayer 的 [Getter] d_BeingPluggedByTeam（被隊友插電中），要分死活由讀的那端自己 AND v_IsDead。
    /// </summary>
    public class IsDealerOnReceiverCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        [SerializeField]
        private GeneralEffectReceiver _receiver;
        protected override bool IsValid => _receiver.HasDealerOverlap;

        public override string Description =>
            $"Has Dealer On [{(_receiver != null ? _receiver.name : "?")}]";
    }
}
