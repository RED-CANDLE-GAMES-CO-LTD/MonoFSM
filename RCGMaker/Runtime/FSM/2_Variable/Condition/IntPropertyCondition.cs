using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    
    //選一個game flag 的int property
    public class IntPropertyCondition : AbstractFieldConditionComp<int, ScriptableObject>
    {
        public Operator Op;

        protected override bool isValid
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