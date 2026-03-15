using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction
{
    //SetObjectValue?
    public class AssignMonoVarValueAction : AbstractStateAction
    {
        public override string Description => $"Set {_targetVar.name} to {_sourceVar.name}";

        //FIXME: validate: 兩個要同型別? 還是value type要可以assign上去就好？
        [SerializeField]
        private AbstractMonoVariable _targetVar;

        //override Object fieldType?
        [SerializeField]
        private AbstractMonoVariable _sourceVar;

        protected override void OnActionExecuteImplement()
        {
            _targetVar.SetValueFromVar(_sourceVar, this);
        }
    }
}
