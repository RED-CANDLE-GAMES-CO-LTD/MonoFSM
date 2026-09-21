namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// 比較一顆 VarInt 跟另一個整數（可以是另一顆 VarInt，也可以是直接填的常數）。
    /// 要用「數量／次數／kind 編號」當閘門時掛這顆，兩邊都用 VarIntWrapper：
    /// 填了 <c>_var</c> 就讀那顆變數，留空就讀 <c>_tempValue</c> 常數。
    /// </summary>
    public class VarIntCompareCondition : AbstractConditionBehaviour
    {
        protected override bool IsValid =>
            ArithmeticHelper.CompareValues(_varInt.Value, _targetValue.Value, _op);

        public VarIntWrapper _varInt;
        public Operator _op;
        public VarIntWrapper _targetValue;

        public override string Description =>
            $"{_varInt} {ArithmeticHelper.OperatorDescription(_op)} {_targetValue}";
    }

    public static class ArithmeticHelper
    {
        public static bool CompareValues(float value1, float value2, Operator op) =>
            op switch
            {
                Operator.Equals => value1 == value2,
                Operator.NotEqual => value1 != value2,
                Operator.GreaterThan => value1 > value2,
                Operator.LessThan => value1 < value2,
                Operator.GreaterThanOrEqual => value1 >= value2,
                Operator.LessThanOrEqual => value1 <= value2,
                _ => false,
            };

        public static string OperatorDescription(Operator op) =>
            op switch
            {
                Operator.Equals => "==",
                Operator.NotEqual => "!=",
                Operator.GreaterThan => ">",
                Operator.LessThan => "<",
                Operator.GreaterThanOrEqual => ">=",
                Operator.LessThanOrEqual => "<=",
                _ => "",
            };
    }
}
