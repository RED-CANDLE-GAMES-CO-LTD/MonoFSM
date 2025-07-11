namespace MonoFSM.Variable.Condition
{
    // 選到一個GameFlagBase的bool property
    public class BoolPropertyOfGameFlagCondition : AbstractFieldConditionBehaviour<bool, GameFlagBase>
    {
        protected override bool IsValid 
            => SourceValue == TargetValue;

        public override string Description 
            => $"{sourceObject.name} {propertyName} is {TargetValue}";
    }
}