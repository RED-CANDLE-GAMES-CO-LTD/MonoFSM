using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;

namespace RCGMaker.Runtime.Interact.EffectHit.Condition
{
    public class IsEffectTypeInMonoDescriptableCondition:AbstractConditionComp
    {
        public GeneralEffectType effectType;
        [Required]
        [DropDownRef]
        public VariableMonoDescriptable targetMonoDescriptableVariable;
        protected override bool isValid => targetMonoDescriptableVariable.Value != null && targetMonoDescriptableVariable.Value.IsEffectTypeIn(effectType);
    }
}