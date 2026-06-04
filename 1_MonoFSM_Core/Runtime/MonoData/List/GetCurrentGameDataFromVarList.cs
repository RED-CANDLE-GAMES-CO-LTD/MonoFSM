using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Variable;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    /// <summary>
    /// 通用：把「某個 VarList 目前 index 指向的 CurrentItem」當作 ValueSource&lt;TItem&gt; 提供。
    /// 與「直接讀 VarList.CurrentListItem」的差別在於：這是個獨立元件，可透過 DropDownRef
    /// 引用「遠端」的 VarList（不必掛在 VarList 同物件），並能接上 ValueSource 的 IsValid / ConditionGroup 多型機制。
    /// 注意：Unity 泛型 MonoBehaviour 無法直接掛，需提供具體型別子類（如 <see cref="GetCurrentGameDataFromVarList"/>）。
    /// </summary>
    public abstract class AbstractGetCurrentItemFromVarList<TItem> : AbstractValueSource<TItem>
    {
        protected abstract VarList<TItem> VarList { get; }

        public override TItem Value => VarList != null ? VarList.CurrentListItem : default;
    }

    /// <summary>
    /// 取得 VarListData 目前的 CurrentItem(GameData)，同時實作 IGameDataProvider，
    /// 可直接被 GetMonoPoolObjFromDescriptableData 以 [Auto] 接續取得 bindPrefab(MonoObj)。
    /// </summary>
    public class GetCurrentGameDataFromVarList
        : AbstractGetCurrentItemFromVarList<GameData>, IGameDataProvider
    {
        [Required]
        [DropDownRef]
        public VarListData _varListData;

        protected override VarList<GameData> VarList => _varListData;

        public GameData GameData => Value;

        public override string Description =>
            _varListData != null ? _varListData.name + " CurrentItem" : "?";
    }
}
