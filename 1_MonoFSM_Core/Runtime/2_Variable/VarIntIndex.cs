using MonoFSM.Core.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

// 專屬於 VarList 的 index 游標變數。
// 用獨立型別讓 VarList 的 [AutoChildren] 能無歧義地抓到「這顆是 index」，
// 而不會誤抓 children 裡其他用途的 VarInt。掛在 VarList 子物件上即自動接上，
// 不需要再透過 valueSource 手動綁定。
public class VarIntIndex : VarInt
{
    // 往上抓所屬的 VarList（另一端用 [AutoChildren] 把這顆當 index）。
    [ShowInInspector] [AutoParent] private AbstractVarList _ownerList;

    public override int CurrentValue
    {
        get
        {
            // owner 進入 proxy 模式時，effective index 在 proxy 那顆 VarList，本地存的值會 stale，
            // 改讀 owner.CurrentIndex（它會 forward 給 proxy）。
            // 非 proxy 模式：本地值才是 source of truth，直接走 base —
            // 不能無條件 forward，否則 owner.CurrentIndex → RawIndex → 這顆 CurrentValue 互相遞迴。
            if (Application.isPlaying && _ownerList != null && _ownerList.IsProxy)
                return _ownerList.CurrentIndex;
            return base.CurrentValue;
        }
    }
}
