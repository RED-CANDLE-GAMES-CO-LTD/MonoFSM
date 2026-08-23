using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    /// <summary>
    ///     商店機台的庫存制：把「當前選到的商品」標記成已售出，接在購買成功的 state 上。
    ///     售出狀態是一顆 int bitmask（bit index = 商品在清單裡的 index），存在機台上，
    ///     所以是全機台共享的存貨 —— 三份存貨可以一個人買三次，也可以三個人各買一份。
    ///     配 ItemSoldOutCondition 擋重複購買。
    /// </summary>
    public class MarkItemSoldOutAction : AbstractStateAction
    {
        [DropDownRef]
        [SerializeField]
        [Tooltip("機台上記錄哪些商品已售出的 bitmask（VarInt）")]
        private VarInt _soldOutMask;

        [DropDownRef]
        [SerializeField]
        [Tooltip("當前選到第幾個商品（機台既有的 Item Index）")]
        private VarInt _itemIndex;

        public override string Description => "Mark Current Item Sold Out";

        protected override void OnActionExecuteImplement()
        {
            if (_soldOutMask == null || _itemIndex == null)
            {
                Debug.LogError("[MarkItemSoldOut] _soldOutMask 或 _itemIndex 沒設", this);
                return;
            }

            var index = _itemIndex.Get<int>();
            if (index < 0 || index >= 32)
            {
                //bitmask 是 int，只放得下 32 個商品
                Debug.LogError($"[MarkItemSoldOut] index {index} 超出 bitmask 範圍（0~31）", this);
                return;
            }

            _soldOutMask.SetRaw(_soldOutMask.Get<int>() | (1 << index), this);
        }
    }
}
