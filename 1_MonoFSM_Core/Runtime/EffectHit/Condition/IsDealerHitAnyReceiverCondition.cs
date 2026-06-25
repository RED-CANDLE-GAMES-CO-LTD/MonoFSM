using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
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

