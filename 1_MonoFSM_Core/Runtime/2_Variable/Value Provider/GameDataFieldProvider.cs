using System;
using MonoFSM.Core.Utilities;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.DataProvider
{
    //簡化版 ValueProvider：直接組合一顆 VarGameData，用 FieldPath 下拉選 GameData 的欄位
    //型別跟著 FieldPath 末端走（不像 GameDataFieldStringValueSource 固定 string），給需要原始型別的 binder 用
    public class GameDataFieldProvider : PropertyOfTypeProvider
    {
        [Required]
        [PropertyOrder(0)]
        [DropDownRef]
        public VarGameData _gameData;

        public override object StartingObject => _gameData != null ? _gameData.Value : null;

        //優先用 VarTag 的 ValueFilterType（可看到 GameData 子類別的欄位），否則退回 GameData
        public override Type GetObjectType
        {
            get
            {
                var filterType = _gameData?._varTag?.ValueFilterType;
                if (filterType != null && typeof(GameData).IsAssignableFrom(filterType))
                    return filterType;
                return typeof(GameData);
            }
        }

        public override Type ValueType => HasFieldPath ? lastPathEntryType : GetObjectType;

        public override T1 Get<T1>()
        {
            var data = _gameData != null ? _gameData.Value : null;
            if (data == null)
                return default;

            if (!HasFieldPath)
                return data is T1 t1 ? t1 : default;

            var (fieldValue, _) = ReflectionUtility.GetFieldValueFromPath<T1>(
                data,
                _pathEntries,
                gameObject
            );
            return fieldValue;
        }

        public override string Description =>
            $"{(_gameData != null ? _gameData.name : "?")}.{PropertyPath}";

        protected override string DescriptionTag => "Value";
    }
}
