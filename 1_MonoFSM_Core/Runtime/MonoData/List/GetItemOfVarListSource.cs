using System;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    /// <summary>
    /// 無型別耶
    /// 取得 VarList 指定 index 的項目；_index 為 -1 時取 current index 的項目
    /// </summary>
    public class GetItemOfVarListSource : AbstractGetter, IValueProvider
    {
        public override string Description =>
            (_index.Value < 0 ? "Current of" : $"Item[{_index.Value}] of")
            + (_varList != null ? $" {_varList.name}" : " VarList"); //TODO:把[內的東西刪掉]

        [SerializeField]
        private AbstractVarList _varList;

        [Tooltip("-1 表示取 current index 的項目")] [SerializeField]
        private VarIntWrapper _index = new(-1);

        public override bool HasValue =>
            _varList != null && _varList.GetRawObjectAt(_index.Value) != null;

        public T1 Get<T1>()
        {
            if (_varList == null)
                return default;
            if (_varList.GetRawObjectAt(_index.Value) is T1 t1)
                return t1;
            return default;
        }

        public Type ValueType => _varList?.ValueType;
    }
}
