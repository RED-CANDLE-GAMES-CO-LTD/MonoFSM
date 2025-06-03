using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM_Core.Runtime.Action.VariableAction
{
    public class ToggleBoolAction : AbstractStateAction
    {
        [SerializeField] [DropDownRef] public VarBool _target; //var?

        protected override void OnStateEnterImplement()
        {
            _target.SetValue(!_target.Value);
        }
    }
}