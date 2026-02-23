using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    //FIXME: 很怪？
    public class IsVarValueExistCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        public AbstractMonoVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool IsValid => unityObjectVariable.IsValueExist;
    }
}
