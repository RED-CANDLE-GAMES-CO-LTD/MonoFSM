using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    public class IsDealerHitAnyReceiverCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        [SerializeField]
        private GeneralEffectDealer _dealer;
        protected override bool IsValid => _dealer.HasReceiverOverlap;

        public override string Description =>
            $"Dealer ${_dealer.Description} hit any?";
    }
}

