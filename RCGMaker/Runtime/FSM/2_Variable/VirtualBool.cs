namespace RCGMaker.Runtime.FSM._2_Variable
{
    public class VirtualBool : VariableBool
    {
        public VariableBool bindedVariable;
        public override bool FinalValue => CurrentValue;
    }
}