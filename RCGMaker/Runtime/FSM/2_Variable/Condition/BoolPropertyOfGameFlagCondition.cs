using System;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    //選到一個GameFlagBase的bool property
    public class BoolPropertyOfGameFlagCondition : AbstractFieldConditionComp<bool, GameFlagBase>
    {
        protected override bool isValid => SourceValue == TargetValue;
        // (bool)sourceObject.GetType().GetProperty(propertyName).GetValue(sourceObject) == TargetValue;

        // private delegate bool GetBoolProperty(GameFlagBase sourceObject);
        //
        // private GetBoolProperty getBoolProperty;
        //
        // private void Awake()
        // {
        //     getBoolProperty = (GetBoolProperty)Delegate.CreateDelegate(typeof(GetBoolProperty),
        //         sourceObject, propertyName);
        //     
        // }

        protected override string nameDescription =>
            $"[Condition] Flag:{sourceObject.name} {propertyName} == {TargetValue}";
    }
}