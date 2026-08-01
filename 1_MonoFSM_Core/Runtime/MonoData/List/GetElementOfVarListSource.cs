using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    /// <summary>
    /// 無型別耶
    /// 取得 VarList 指定 index 的項目；_index 為 -1 時取 current index 的項目
    /// 沒手動指定 _varList 時，往上找祖先身上的 VarList（讓多個 UI slot 共用同一份清單，
    /// 不用每個 slot 各複製一顆 list getter）。
    /// </summary>
    public class GetElementOfVarListSource : AbstractGetter, IValueProvider
    {
        public override string Description =>
            (_index.Value < 0 ? "Current of" : $"Item[{_index.Description}] of")
            + (TargetList != null ? $" {TargetList.name}" : " VarList"); //TODO:把[內的東西刪掉]

        [SerializeField]
        private AbstractVarList _varList;

        // [AutoParent] 是無條件覆寫，不能直接標在 _varList 上（會洗掉手動指定的引用），
        // 所以另開一顆 fallback 欄位。找不到不報錯：手動指定 _varList 是正常用法。
        [SerializeField] [AutoParent(false)] private AbstractVarList _parentVarList;

        [ShowInInspector]
        private AbstractVarList TargetList => _varList != null ? _varList : _parentVarList;

        [Tooltip("-1 表示取 current index 的項目")] [SerializeField]
        private VarIntWrapper _index = new(-1);

        public override bool HasValue =>
            TargetList != null && TargetList.GetRawObjectAt(_index.Value) != null;

        public T1 Get<T1>()
        {
            var list = TargetList;
            if (list == null)
            {
                Debug.Log("[GetElementOfVarListSource] 沒有 VarList 可取值（_varList 與祖先都沒有）", this);
                return default;
            }

            if (list.GetRawObjectAt(_index.Value) is T1 t1)
                return t1;
            return default;
        }

        public Type ValueType => TargetList?.ValueType;
    }
}
