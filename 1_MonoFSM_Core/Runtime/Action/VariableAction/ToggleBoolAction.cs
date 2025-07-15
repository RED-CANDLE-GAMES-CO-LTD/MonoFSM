using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Action.VariableAction
{
    public class ToggleBoolAction : AbstractStateAction
    {
        [SerializeField] [DropDownRef] public VarBool _target; //var?

        protected override void OnActionExecuteImplement()
        {
            // Debug.Log($"ToggleBoolAction: Toggling value of {_target}", this);
            _target.SetValue(!_target.Value, this);
        }

        public override string Description =>
            _target != null ? $"Toggle Bool: {_target.name}" : "Toggle Bool: No target set";
    }
}