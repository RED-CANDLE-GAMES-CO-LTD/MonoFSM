using System;

namespace RCGMaker.Runtime.FSM._2_Variable.VariableBinder
{
    public class VariableBoolRebindEntry : VariableBindingEntry<VariableBool>
    {
        public override void Bind()
        {
            // WatchSource.Field.AddListener(value => { dependentVariable.SetValue(value, this); }, this);
            WatchSource.Field.AddListener(OnValueChange, this);
        }

        private void OnValueChange(bool value)
        {
            dependentVariable.SetValue(value, this);
        }

        private void OnDestroy()
        {
            WatchSource.Field.RemoveListener(OnValueChange, this);
        }
    }
}