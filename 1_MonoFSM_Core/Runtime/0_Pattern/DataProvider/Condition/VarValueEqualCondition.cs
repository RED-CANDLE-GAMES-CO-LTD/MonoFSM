using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.VarRefOld;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.DataProvider.Condition
{
    /// <summary>
    ///     兩顆變數「現在的值是不是同一個」——走 AbstractMonoVariable.EqualsVar，
    ///     所以 VarEntity / VarComp 這類物件型比的是同一個 reference，數值型比值。
    ///     配 FinalResultInverted 就是「不等於」（例如：這次碰到的不是上次放開的那顆）。
    /// </summary>
    public class VarValueEqualCondition : AbstractConditionBehaviour //
    {
        [DropDownRef] [SerializeField] private AbstractMonoVariable _targetVarRef;
        [DropDownRef] [SerializeField] private AbstractMonoVariable _sourceValueRef;

        [Tooltip("勾選時，當兩者的值都為 null（不存在）時不算 Valid")] [SerializeField]
        private bool _treatBothNullAsInvalid = true;

        protected override bool IsValid
        {
            get
            {
                if (_treatBothNullAsInvalid
                    && !_targetVarRef.IsValueExist
                    && !_sourceValueRef.IsValueExist)
                    return false;
                return _targetVarRef.EqualsVar(_sourceValueRef);
            }
        }

        public override string Description => $"{_sourceValueRef} == {_targetVarRef}";
    }
}
