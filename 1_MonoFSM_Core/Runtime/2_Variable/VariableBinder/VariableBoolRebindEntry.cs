using UnityEngine;

namespace MonoFSM.Variable.VariableBinder
{
    public class VariableBoolRebindEntry : VariableBindingEntry<VarBool>
    {
        public override void Bind()
        {
            // WatchSource.Field.AddListener(value => { dependentVariable.SetValue(value, this); }, this);
            Debug.Log("Bind " + WatchSource.name + " " + dependentVariable.name);
            WatchSource.Field.AddListener(OnValueChange, this); //FIXME: 這個如果可以抽出來更好？
            // WatchSource.OverrideTarget(dependentVariable);
            WatchSource.SetBindingTarget(dependentVariable);
            dependentVariable.SetBindingSource(WatchSource);
        }

        private void OnValueChange(bool value)
        {
            Debug.Log("OnValueChange " + WatchSource.name + " " + dependentVariable.name);
            dependentVariable.SetValue(value, this);
        }

        private void OnDestroy()
        {
            WatchSource.Field.RemoveListener(OnValueChange, this);
        }
    }
}