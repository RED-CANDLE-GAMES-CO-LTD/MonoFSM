using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VariableBoolValueCondition : AbstractConditionComp
    {
        protected override string nameDescription => _monoVariableBool.name + " == " + targetValue;

        VariableBool[] GetBoolVariables()
        {
            return this.GetComponentsInBinder<VariableBool>();
        }
        //FIXME: 好像可以再簡化喔

        [FormerlySerializedAs("variableBool")] [Required] [DropDownRef]
        // [ValueDropdown(nameof(GetBoolVariables))]
        public VariableBool _monoVariableBool;

        public bool targetValue = true;
        protected override bool isValid => _monoVariableBool.CurrentValue == targetValue;
    }
}