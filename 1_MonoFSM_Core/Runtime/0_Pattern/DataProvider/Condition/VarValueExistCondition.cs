using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.Condition
{
    /// <summary>
    ///     「這顆變數 runtime 上真的有值嗎」——走 AbstractMonoVariable.IsValueExist，
    ///     所以 VarEntity / VarComp 這類物件型可以分得出「還沒被指派」與「指派成 null」。
    ///     要判斷「沒有值」就開 FinalResultInverted，不要另外做一顆條件。
    ///     與 MonoFSM.Variable.Condition.IsVarValueExistCondition 功能重複（舊的那顆），新做的用這顆。
    /// </summary>
    public class VarValueExistCondition : AbstractConditionBehaviour
    {
        [DropDownRef] [SerializeField] private AbstractMonoVariable _targetVariable;

        protected override bool IsValid => _targetVariable?.IsValueExist ?? false;
        public override string Description => $"Var: {_targetVariable?.name} Value Exist";
    }
}
