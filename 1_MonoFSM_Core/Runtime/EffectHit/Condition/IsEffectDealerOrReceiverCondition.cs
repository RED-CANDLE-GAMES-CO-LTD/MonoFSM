using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    public class IsEffectDealerOrReceiverCondition : AbstractConditionBehaviour
    {
        public enum EffectSide
        {
            Dealer,
            Receiver,
        }

        public override string Description =>
            $"Is [{effectSide}] Of Type [{effectType.name}] under [{(_targetEntityDescriptableVar.name)}]";

        [FormerlySerializedAs("_targetBlackboardDescriptableVar")]
        [FormerlySerializedAs("_targetMonoDescriptableVar")]
        [FormerlySerializedAs("targetMonoDescriptableVariable")]
        [Required]
        [DropDownRef]
        public VarEntity _targetEntityDescriptableVar;

        [Header("的")] public EffectSide effectSide;
        [Header("有")] public GeneralEffectType effectType;

        protected override bool IsValid
        {
            get
            {
                if (_targetEntityDescriptableVar.Value == null)
                    return false;
                if (effectSide == EffectSide.Dealer)
                    return _targetEntityDescriptableVar.Value.HasDealerType(effectType);
                else
                    return _targetEntityDescriptableVar.Value.HasReceiverType(effectType);
            }
        }


    }
}
