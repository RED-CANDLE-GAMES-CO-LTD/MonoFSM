using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    public class SetLocalPositionAction : AbstractStateAction
    {
        public Transform _target;
        public VarVector3 _targetPosVar;

        protected override void OnActionExecuteImplement()
        {
            _target.localPosition = _targetPosVar.Value;
        }
    }
}
