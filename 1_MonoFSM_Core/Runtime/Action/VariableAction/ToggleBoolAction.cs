using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    public class ToggleBoolAction : AbstractStateAction
    {
        [SerializeField] [DropDownRef] public VarBool _target; //var?

        protected override void OnStateEnterImplement()
        {
            // Debug.Log($"ToggleBoolAction: Toggling value of {_target}", this);
            _target.SetValue(!_target.Value);
        }
    }
}