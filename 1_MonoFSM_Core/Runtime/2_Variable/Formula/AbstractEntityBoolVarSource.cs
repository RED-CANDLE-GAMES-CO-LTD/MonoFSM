using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Variable;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    /// <summary>
    ///     共用基底：對一份 entity 清單讀取某個 bool 變數
    ///     子類決定怎麼聚合 / 篩選（count、and、filter...）
    /// </summary>
    public abstract class AbstractEntityBoolVarSource<T> : AbstractValueSource<T>
    {
        [Tooltip("來源清單")]
        public VarListEntity _entities;

        [Tooltip("要檢查的 bool 變數")]
        [SOConfig("VariableType")]
        public VariableTag _boolVarTag; //hmm??

        /// <summary>
        ///     取來源清單，拿不到就回 null（由子類決定 early return 要回什麼）
        /// </summary>
        protected List<MonoEntity> GetSourceList()
        {
            if (_entities == null)
                return null;

            return _entities.GetList();
        }

        /// <summary>
        ///     讀 entity 上的 bool 變數，沒有這顆變數就回 false（value 也是 false）
        ///     var 被 disable 或所在物件 inactive 時也回 false，視為「不參與統計」
        /// </summary>
        protected bool TryGetBool(MonoEntity entity, out bool value)
        {
            value = false;
            if (entity == null || _boolVarTag == null)
                return false;

            var boolVar = entity.GetVar<VarBool>(_boolVarTag);
            if (boolVar == null)
                return false;

            //disable / inactive 的 var 會留著上次的殘值，不能採信
            if (!boolVar.enabled || !boolVar.gameObject.activeInHierarchy)
                return false;

            value = boolVar.Value;
            return true;
        }
    }
}
