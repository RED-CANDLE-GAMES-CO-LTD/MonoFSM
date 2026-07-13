using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    public class IsVarValueExistCondition : AbstractConditionBehaviour
    {
        public override string Description => $"Is {unityObjectVariable?.name} exist?";

        [DropDownRef]
        public AbstractMonoVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool IsValid => unityObjectVariable?.IsValueExist ?? false;
    }
}
