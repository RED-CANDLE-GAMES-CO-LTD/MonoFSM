using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.MonoData;
using MonoFSM.Condition;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable.Condition
{
    /// <summary>
    /// 比對一顆 VarGameData 的目前值是否為指定 GameData。
    /// 目標值的 Inspector 下拉選項來自指定的 VarListData，適合用同一份資料表同時驅動
    /// index-to-GameData getter 與條件設定，避免工具種類增刪後手動維護另一份 enum 或清單。
    /// </summary>
    public class VarGameDataCompareCondition : AbstractConditionBehaviour
    {
        [Required]
        [DropDownRef]
        [SerializeField]
        [OnValueChanged(nameof(OnConfigurationChanged))]
        private VarGameData _currentData;

        [Required]
        [DropDownRef]
        [SerializeField]
        [OnValueChanged(nameof(OnConfigurationChanged))]
        private VarListData _gameDataMap;

        [SerializeField]
        [ValueDropdown(nameof(GetTargetDataOptions), NumberOfItemsBeforeEnablingSearch = 8, OnlyChangeValueOnConfirm = true)]
        [OnValueChanged(nameof(OnConfigurationChanged))]
        private GameData _targetData;

        public override string Description =>
            $"{(_currentData != null ? _currentData.PathName : "?")} == {GetTargetDescription()}";

        protected override bool IsValid =>
            _currentData != null && _currentData.Value == _targetData;

        private IEnumerable<ValueDropdownItem<GameData>> GetTargetDataOptions()
        {
            if (_gameDataMap == null)
                yield break;

            var index = 0;
            foreach (var item in _gameDataMap.GetItems())
            {
                var label = item != null ? item.name : "None";
                yield return new ValueDropdownItem<GameData>($"{index}: {label}", item);
                index++;
            }
        }

        private string GetTargetDescription()
        {
            if (_targetData != null)
                return _targetData.name;

            if (_gameDataMap != null)
            {
                foreach (var item in _gameDataMap.GetItems())
                {
                    if (item == null)
                        return "None";
                }
            }

            return "?";
        }

        private void OnConfigurationChanged()
        {
            Rename();
        }
    }
}
