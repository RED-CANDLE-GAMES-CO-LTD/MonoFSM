using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //選一個game flag 的int property
    public class IntPropertyCondition : AbstractFieldConditionComp<int>
    {
        public Operator Op;

        protected override bool isValid
        {
            get
            {
                switch (Op)
                {
                    case Operator.Equals:
                        return SourceValue == TargetValue;
                    case Operator.NotEqual:
                        return SourceValue != TargetValue;
                    case Operator.GreaterThan:
                        return SourceValue > TargetValue;
                    case Operator.LessThan:
                        return SourceValue < TargetValue;
                }

                return false;
            }
        }
    }
}