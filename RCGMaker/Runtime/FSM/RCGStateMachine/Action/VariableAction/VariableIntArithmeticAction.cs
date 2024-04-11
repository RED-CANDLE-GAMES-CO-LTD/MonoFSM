namespace RCGFSM.Variable
{
    public class VariableIntArithmeticAction : AbstractStateAction
    {
        public VariableInt target;
        public ArithmeticOperator Arithmetic;
        public int Value;

        protected override void OnStateEnterImplement()
        {
            this.Log("Arithmetic: ", Arithmetic, " Value: ", Value);
            switch (Arithmetic)
            {
                case ArithmeticOperator.Add:
                    target.SetValue(target.Value + Value, this);
                    break;
                case ArithmeticOperator.Sub:
                    target.SetValue(target.Value - Value, this);
                    break;
                case ArithmeticOperator.Mul:
                    target.SetValue(target.Value * Value, this);
                    break;
                case ArithmeticOperator.Div:
                    target.SetValue(target.Value / Value, this);
                    break;
            }
        }
    }
}