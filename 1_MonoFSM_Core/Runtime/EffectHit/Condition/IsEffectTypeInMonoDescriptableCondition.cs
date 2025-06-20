using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Runtime.Interact.EffectHit.Condition
{
    //FIXME: 這個是MonoDescribable下面有Receiver有EffectType
    //可以被 xx Effect 作用

    public class IsEffectTypeInMonoDescriptableCondition : AbstractConditionComp
    {
        public enum EffectSide
        {
            Dealer,
            Receiver,
        }


        [FormerlySerializedAs("_targetMonoDescriptableVar")]
        [FormerlySerializedAs("targetMonoDescriptableVariable")]
        [Required]
        [DropDownRef]
        public VarBlackboard _targetBlackboardDescriptableVar;

        [Header("的")] public EffectSide effectSide;
        [Header("有")] public GeneralEffectType effectType;

        protected override bool IsValid
        {
            get
            {
                if (_targetBlackboardDescriptableVar.Value == null)
                    return false;
                if (effectSide == EffectSide.Dealer)
                    return _targetBlackboardDescriptableVar.Value.HasDealerType(effectType);
                else
                    return _targetBlackboardDescriptableVar.Value.HasReceiverType(effectType);
            }
        }
    }
}