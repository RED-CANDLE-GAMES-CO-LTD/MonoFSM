using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    ///     商店機台的庫存制：判斷「當前選到的第幾個商品」有沒有被賣掉，賣掉的不能再買。
    ///     售出狀態是一顆 int bitmask（bit index = 商品在清單裡的 index），存在機台上，
    ///     所以是全機台共享的存貨 —— 誰買都會消耗掉同一份，跟買的人是誰無關。
    ///     _expectSoldOut = false 給購買條件用（沒賣掉才能買），true 給「已售出」文字提示用。
    /// </summary>
    public class ItemSoldOutCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        [SerializeField]
        [Tooltip("機台上記錄哪些商品已售出的 bitmask（VarInt）")]
        private VarInt _soldOutMask;

        [DropDownRef]
        [SerializeField]
        [Tooltip("當前選到第幾個商品（機台既有的 Item Index）")]
        private VarInt _itemIndex;

        [SerializeField]
        [Tooltip("勾起來＝「已售出時成立」（文字提示用）；不勾＝「還有存貨才成立」（購買條件用）")]
        private bool _expectSoldOut;

        protected override bool IsValid
        {
            get
            {
                if (_soldOutMask == null || _itemIndex == null)
                {
                    Debug.LogError("[ItemSoldOut] _soldOutMask 或 _itemIndex 沒設", this);
                    return false;
                }

                var index = _itemIndex.Get<int>();
                if (index < 0 || index >= 32)
                {
                    //bitmask 是 int，只放得下 32 個商品
                    Debug.LogError($"[ItemSoldOut] index {index} 超出 bitmask 範圍（0~31）", this);
                    return false;
                }

                var isSoldOut = (_soldOutMask.Get<int>() & (1 << index)) != 0;
                return isSoldOut == _expectSoldOut;
            }
        }

        public override string Description =>
            _expectSoldOut ? "Item Is Sold Out" : "Item In Stock";
    }
}
