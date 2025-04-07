using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable.Condition
{
    //選一個game flag 的int property
    public class IntPropertyCondition : AbstractFieldConditionComp<int, ScriptableObject>
    {
        protected override bool IsShowRenameButton => true;

        protected override string Description =>
            sourceObject.name + "." + propertyName + " " + Op + " " + TargetValue;

        public Operator Op;

        protected override bool IsValid
        {
            get
            {
                return Op switch
                {
                    Operator.Equals => SourceValue == TargetValue,
                    Operator.NotEqual => SourceValue != TargetValue,
                    Operator.GreaterThan => SourceValue > TargetValue,
                    Operator.LessThan => SourceValue < TargetValue,
                    _ => false
                };
            }
        }
    }
}