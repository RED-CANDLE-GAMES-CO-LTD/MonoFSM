using System;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Utilities;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.DataProvider
{
    //ValueSource<string> 版本：組合一顆 VarGameData，用 FieldPath 下拉選 GameData 的欄位，最終輸出 string
    //維持 AbstractValueSource 的做法（ConditionGroup、ValueInfo、固定型別），FieldPath 機制自帶
    public class GameDataFieldStringValueSource : AbstractValueSource<string>, IFieldPathRootTypeProvider
    {
        [Required]
        [PropertyOrder(0)]
        [DropDownRef]
        public VarGameData _gameData;

        [FieldPathEditor]
        [BoxGroup("Field Path", ShowLabel = true)]
        [PropertyOrder(1)]
        [OnValueChanged(nameof(OnPathEntriesChanged))]
        public List<FieldPathEntry> _pathEntries = new();

        private bool HasFieldPath => _pathEntries is { Count: > 0 };

        //FieldPath 的起始型別：優先用 VarTag 限制的子類別，否則 GameData
        public Type GetFieldPathRootType()
        {
            var filterType = _gameData?._varTag?.ValueFilterType;
            if (filterType != null && typeof(GameData).IsAssignableFrom(filterType))
                return filterType;
            return typeof(GameData);
        }

        private void OnPathEntriesChanged()
        {
            ReflectionUtility.UpdatePathEntryTypes(_pathEntries, GetFieldPathRootType());
        }

        public override bool HasValue => _gameData != null && _gameData.Value != null;

        public override string Value
        {
            get
            {
                if (!HasValue)
                    return null;
                var data = _gameData.Value;
                if (!HasFieldPath)
                    return data.ToString();
                //末端欄位可能是 int/float 等，統一 ToString 成 string 輸出
                var (fieldValue, _) = ReflectionUtility.GetFieldValueFromPath<object>(
                    data,
                    _pathEntries,
                    gameObject
                );
                return fieldValue?.ToString();
            }
        }

        public override string Description =>
            $"{(_gameData != null ? _gameData.name : "?")}.{string.Join(".", _pathEntries.ConvertAll(e => e.PropertyPath ?? "?"))}";
    }
}
