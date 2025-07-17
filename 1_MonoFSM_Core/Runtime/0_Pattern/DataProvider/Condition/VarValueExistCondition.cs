using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.Condition
{
    public class VarValueExistCondition : AbstractConditionBehaviour
    {
        [DropDownRef] [SerializeField] private AbstractMonoVariable _targetVariable;

        protected override bool IsValid => _targetVariable.IsValueExist;
        public override string Description => $"Var: {_targetVariable.name} Value Exist";
    }
}