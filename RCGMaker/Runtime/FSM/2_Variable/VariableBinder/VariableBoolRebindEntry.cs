using System;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable.VariableBinder
{
    public class VariableBoolRebindEntry : VariableBindingEntry<VariableBool>
    {
        public override void Bind()
        {
            // WatchSource.Field.AddListener(value => { dependentVariable.SetValue(value, this); }, this);
            Debug.Log("Bind");
            WatchSource.Field.AddListener(OnValueChange, this);
            // WatchSource.OverrideTarget(dependentVariable);
            WatchSource.SetBindingTarget(dependentVariable);
            dependentVariable.SetBindingSource(WatchSource);
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