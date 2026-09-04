using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.EffectHit.Resolver
{
    /// <summary>
    ///     不經 detector、直接讓一組 dealer → receiver 發一次 effect（走 ForceDirectEffectHit，enter 完立刻 exit）。
    ///     跟 EffectDetector 的差別：這裡不判重疊、不看距離，「誰打誰」是自己指定的，
    ///     所以用在「偵測與施加要分開」的場合 —— 例如 passive dealer 平常只維護命中清單，
    ///     玩家按下使用時才用這顆對清單裡的每個目標各發一次（配 ForEachEntityInListAction）。
    ///     dealer / receiver 兩端都可以選 DirectReference 或 FromVarEntity（後者用 _effectType 去該 entity 上查）。
    /// </summary>
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
