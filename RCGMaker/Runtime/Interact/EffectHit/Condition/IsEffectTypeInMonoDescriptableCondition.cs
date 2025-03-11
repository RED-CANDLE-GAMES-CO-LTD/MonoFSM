using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.Interact.EffectHit.Condition
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


        [FormerlySerializedAs("targetMonoDescriptableVariable")] [Required] [DropDownRef]
        public VarMono _targetMonoDescriptableVar;

        [Header("的")] public EffectSide effectSide;
        [Header("有")] public GeneralEffectType effectType;

        protected override bool IsValid
        {
            get
            {
                if (_targetMonoDescriptableVar.Value == null)
                    return false;
                if (effectSide == EffectSide.Dealer)
                    return _targetMonoDescriptableVar.Value.HasDealerType(effectType);
                else
                    return _targetMonoDescriptableVar.Value.HasReceiverType(effectType);
            }
        }
    }
}