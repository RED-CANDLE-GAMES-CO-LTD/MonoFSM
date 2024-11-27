using Sirenix.OdinInspector;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VariableBoolValueCondition : AbstractConditionComp
    {
        VariableBool[] GetBoolVariables()
        {
           return this.GetComponentsInBinder<VariableBool>();
        }
        //FIXME: 好像可以再簡化喔
        
        [Required]
        [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VariableBool variableBool;
        public bool targetValue = true;
        protected override bool isValid => variableBool.CurrentValue == targetValue;
    }
}