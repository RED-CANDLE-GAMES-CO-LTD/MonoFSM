using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class BoolPropertyOfGameFlag : AbstractFieldConditionComp<bool, GameFlagBase>
    {
        protected override bool isValid =>
            (bool)sourceObject.GetType().GetProperty(propertyName).GetValue(sourceObject) == TargetValue;
    }
}