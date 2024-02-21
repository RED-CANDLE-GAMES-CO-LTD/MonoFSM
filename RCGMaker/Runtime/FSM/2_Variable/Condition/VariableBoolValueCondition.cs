namespace RCGMaker.Runtime.FSM._2_Variable.Condition
{
    public class VariableBoolValueCondition : AbstractConditionComp
    {
        public VariableBool variableBool;
        public bool targetValue;
        protected override bool isValid => variableBool.Value == targetValue;
    }
}