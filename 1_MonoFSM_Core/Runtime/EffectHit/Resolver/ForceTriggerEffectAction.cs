using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.EffectHit.Resolver
{
    public class ForceTriggerEffectAction : AbstractStateAction
    {
        enum SourceMode { DirectReference, FromVarEntity }

        // ── Dealer ──
        [SerializeField] SourceMode _dealerSource;

        [ShowIf(nameof(_dealerSource), SourceMode.DirectReference)]
        [Required]
        [DropDownRef]
        public GeneralEffectDealer _dealer;

        [ShowIf(nameof(_dealerSource), SourceMode.FromVarEntity)]
        [Required]
        [DropDownRef]
        public VarEntity _dealerEntity;

        // ── Receiver ──
        [SerializeField] SourceMode _receiverSource;

        [ShowIf(nameof(_receiverSource), SourceMode.DirectReference)]
        [Required]
        [DropDownRef]
        public GeneralEffectReceiver _receiver;

        [ShowIf(nameof(_receiverSource), SourceMode.FromVarEntity)]
        [Required]
        [DropDownRef]
        public VarEntity _receiverEntity;

        // ── EffectType (FromVarEntity 時用來查找) ──
        bool NeedEffectType => _dealerSource == SourceMode.FromVarEntity || _receiverSource == SourceMode.FromVarEntity;

        [ShowIf(nameof(NeedEffectType))]
        [Required]
        public GeneralEffectType _effectType;

//FIXME: 可以從有DirectReference的那個拿吧
        protected override void OnActionExecuteImplement()
        {
            var dealer = _dealerSource == SourceMode.DirectReference
                ? _dealer
                : _dealerEntity.Value?.GetDealer(_effectType);

            var receiver = _receiverSource == SourceMode.DirectReference
                ? _receiver
                : _receiverEntity.Value?.GetReceiver(_effectType);

            if (dealer == null || receiver == null)
            {
                //不一定有嗎？
                //install vs use
                Debug.LogError($"[ForceTriggerEffect] dealer={dealer}, receiver={receiver}", this);
                return;
            }

            receiver.ForceDirectEffectHit(dealer, null);
        }
    }
}
