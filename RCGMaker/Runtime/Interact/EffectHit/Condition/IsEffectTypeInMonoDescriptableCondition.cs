using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit.Condition
{
    //FIXME: 這個是MonoDescribable下面有Receiver有EffectType
    //可以被 xx Effect 作用
    
    public class IsEffectTypeInMonoDescriptableCondition:AbstractConditionComp
    {
        public enum EffectSide
        {
            Dealer,
            Receiver,
        }
        
        
        [Required]
        [DropDownRef]
        public VariableMonoDescriptable targetMonoDescriptableVariable;
        [Header("的")]
        public EffectSide effectSide;
        [Header("有")]
        public GeneralEffectType effectType;

        protected override bool isValid
        {
            get
            {
                if(targetMonoDescriptableVariable.Value == null)
                    return false;
                if(effectSide == EffectSide.Dealer)
                    return targetMonoDescriptableVariable.Value.HasDealerType(effectType);
                else
                    return targetMonoDescriptableVariable.Value.HasReceiverType(effectType);
            }
        }
    }
}