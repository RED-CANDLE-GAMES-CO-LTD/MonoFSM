namespace MonoFSM.Variable.Condition
{
    public class IntCompareCondition: AbstractConditionBehaviour
    {
        protected override bool IsValid => ArithmeticHelper.CompareValues(_varInt.Value, _targetValue, _op);
        [DropDownRef]
        public VarInt _varInt;
        public Operator _op;
        public int _targetValue;
    }
    
    public static class ArithmeticHelper
    {
        public static bool CompareValues(float value1, float value2, Operator op) 
            => op switch
            {
                Operator.Equals => value1 == value2,
                Operator.NotEqual => value1 != value2,
                Operator.GreaterThan => value1 > value2,
                Operator.LessThan => value1 < value2,
                Operator.GreaterThanOrEqual => value1 >= value2,
                Operator.LessThanOrEqual => value1 <= value2,
                _ => false
            };

        public static string OperatorDescription(Operator op) 
            => op switch
            {
                Operator.Equals => "==",
                Operator.NotEqual => "!=",
                Operator.GreaterThan => ">",
                Operator.LessThan => "<",
                Operator.GreaterThanOrEqual => ">=",
                Operator.LessThanOrEqual => "<=",
                _ => ""
            };
    }
}