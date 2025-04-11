using System;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    //選到一個GameFlagBase的bool property
    public class BoolPropertyOfGameFlagCondition : AbstractFieldConditionComp<bool, GameFlagBase>
    {
        protected override bool IsValid => SourceValue == TargetValue;
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

        protected override string Description =>
            $"{sourceObject.name} {propertyName} is {TargetValue}";
    }
}