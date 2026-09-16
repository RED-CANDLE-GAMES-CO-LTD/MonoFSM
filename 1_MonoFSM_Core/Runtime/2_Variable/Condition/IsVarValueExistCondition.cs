using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    ///     （舊版）判斷一顆變數 runtime 上有沒有值。與
    ///     MonoFSM.Core.DataProvider.Condition.VarValueExistCondition 完全等價，新做的請用那顆。
    /// </summary>
    public class IsVarValueExistCondition : AbstractConditionBehaviour
    {
        public override string Description => $"Is {unityObjectVariable?.name} exist?";

        [DropDownRef]
        public AbstractMonoVariable unityObjectVariable;

        //FIXME: Variable Tag？
        protected override bool IsValid => unityObjectVariable?.IsValueExist ?? false;
    }
}
