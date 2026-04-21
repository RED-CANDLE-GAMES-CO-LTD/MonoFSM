using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    public class SetVarToDefaultAction : AbstractStateAction
    {
        [Required]
        [DropDownRef]
        public AbstractMonoVariable _targetVariable;

        protected override void OnActionExecuteImplement()
        {
            _targetVariable.ResetStateRestore(false);
        }
    }
}
