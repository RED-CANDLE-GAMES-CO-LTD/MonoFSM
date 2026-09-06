using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    /// <summary>
    ///     問「某個 entity 身上有沒有指定 effectType 的 dealer / receiver」。
    ///     _checkMode 決定嚴格度：Exists 只看掛了沒有（型別表裡查得到就算），
    ///     IsValid 再往下問那顆 resolver 現在是不是開著的（isActiveAndEnabled + 自身 conditions 全過）。
    ///     預設 Exists，維持舊行為。
    /// </summary>
    public class IsEffectDealerOrReceiverCondition : AbstractConditionBehaviour
    {
        public enum EffectSide
        {
            Dealer,
            Receiver,
        }

        public enum CheckMode
        {
            /// <summary>掛著就算（舊行為）</summary>
            Exists,

            /// <summary>還要 resolver 自己 IsValid（enabled + conditions 全過）</summary>
            IsValidNow,
        }

        public override string Description =>
            $"Is [{effectSide}] Of Type [{effectType.name}] under [{_targetEntityDescriptableVar.name}]" +
            (_checkMode == CheckMode.IsValidNow ? " And Is Valid" : "");

        [FormerlySerializedAs("_targetBlackboardDescriptableVar")]
        [FormerlySerializedAs("_targetMonoDescriptableVar")]
        [FormerlySerializedAs("targetMonoDescriptableVariable")]
        [Required]
        [DropDownRef]
        public VarEntity _targetEntityDescriptableVar;

        [Header("的")] public EffectSide effectSide;
        [Header("有")] public GeneralEffectType effectType;

        [Header("而且")]
        [Tooltip("Exists = 只要掛了那顆 dealer/receiver 就算過（舊行為）。\n" +
                 "IsValidNow = 那顆 resolver 現在要是開著的：isActiveAndEnabled 且自己身上的 conditions 全過。")]
        [SerializeField]
        private CheckMode _checkMode = CheckMode.Exists;

        /// <summary>
        ///     除錯用：現在這個 entity 身上對應 effectSide / effectType 的那顆 resolver。
        ///     null 代表沒掛（Exists 就已經不成立），有值再看 _debugResolverIsValid 判斷是不是被自身條件擋掉。
        /// </summary>
        [PreviewInInspector]
        private EffectResolver _debugResolver => GetResolver();

        [PreviewInInspector] private bool _debugResolverIsValid => GetResolver()?.IsValid ?? false;

        private EffectResolver GetResolver()
        {
            var entity = _targetEntityDescriptableVar == null
                ? null
                : _targetEntityDescriptableVar.Value;
            if (entity == null || effectType == null)
                return null;

            if (effectSide == EffectSide.Dealer)
                return entity.HasDealerType(effectType) ? entity.GetDealer(effectType) : null;
            return entity.HasReceiverType(effectType) ? entity.GetReceiver(effectType) : null;
        }

        protected override bool IsValid
        {
            get
            {
                var entity = _targetEntityDescriptableVar.Value;
                if (entity == null)
                    return false;

                var hasType = effectSide == EffectSide.Dealer
                    ? entity.HasDealerType(effectType)
                    : entity.HasReceiverType(effectType);
                if (!hasType)
                    return false;
                if (_checkMode == CheckMode.Exists)
                    return true;

                var resolver = GetResolver();
                return resolver != null && resolver.IsValid;
            }
        }
    }
}
