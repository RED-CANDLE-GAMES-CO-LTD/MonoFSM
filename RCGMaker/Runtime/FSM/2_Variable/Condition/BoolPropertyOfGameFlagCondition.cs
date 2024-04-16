using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //選到一個GameFlagBase的bool property
    public class BoolPropertyOfGameFlagCondition : AbstractFieldConditionComp<bool, GameFlagBase>
    {
        protected override bool isValid =>
            (bool)sourceObject.GetType().GetProperty(propertyName).GetValue(sourceObject) == TargetValue;
    }
}