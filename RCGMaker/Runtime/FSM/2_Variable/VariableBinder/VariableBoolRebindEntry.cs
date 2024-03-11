namespace RCGMaker.Runtime.FSM._2_Variable.VariableBinder
{
    public class VariableBoolRebindEntry : VariableBindingEntry<VariableBool>
    {
        public override void Bind()
        {
            WatchSource.Field.AddListener(value => { dependentVariable.SetValue(value, this); }, this);
        }
    }
}