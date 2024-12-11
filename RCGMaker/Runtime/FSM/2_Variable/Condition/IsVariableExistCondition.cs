using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class IsVariableExistCondition:AbstractConditionComp
    {
        [FormerlySerializedAs("unityObjectVariable")] [DropDownRef]
        public AbstractReferenceVariable ComponentVariable;
        protected override bool isValid => ComponentVariable.RawValue != null;
    }
}