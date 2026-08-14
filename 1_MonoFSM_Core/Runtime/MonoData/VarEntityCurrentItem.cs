using MonoFSM.Core.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Variable
{
    /// <summary>
    ///     專屬於 VarListEntity 的「當前項目」鏡射變數：掛在 VarListEntity 的子物件上即自動接上，
    ///     取值恆等於 parent list 的 CurrentListItem，不需要外部化 index 也不需要額外接一顆 provider。
    ///     外部要引用「list 現在選到誰」時直接 DropDownRef 指這顆即可。
    ///     用獨立型別（比照 VarIntIndex）而不是裸 VarEntity，是因為 VarList 的 children 可能有其他用途的
    ///     VarEntity，共用型別會讓自動接線無從分辨。
    ///     取值來源固定是 parent list，不吃自己的 valueSource／_defaultValue（沒有 owner 時才 fallback 回 base）。
    /// </summary>
    public class VarEntityCurrentItem : VarEntity
    {
        // 往上抓所屬的 VarListEntity。owner 進 proxy 模式時 CurrentListItem 自己會 forward，這裡不用處理。
        [ShowInInspector] [AutoParent] private VarList<MonoEntity> _ownerList;

        // 值由 parent list 決定：隱藏 _defaultValue / _isRuntimeOnly，並讓 Inspector 標成 Getter 而非 Var
        public override bool HasProxySource => true;
        protected override string DescriptionTag => "Getter";

        protected override MonoEntity GetValueInternal()
        {
            if (_ownerList == null)
            {
                Debug.LogError(
                    $"{name} 是 VarEntityCurrentItem，但 parent 找不到 VarListEntity，取不到 CurrentItem",
                    this);
                return base.GetValueInternal();
            }

            return _ownerList.CurrentListItem;
        }
    }
}
