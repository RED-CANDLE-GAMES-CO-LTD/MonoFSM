using System;
using System.Collections.Generic;
using System.Linq;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// DataFunctionFilterAttribute 的 OdinAttributeDrawer
    /// 用於過濾含有特定 DataFunction 的 GameData
    /// </summary>
    public class DataFunctionFilterDrawer : OdinAttributeDrawer<DataFunctionFilterAttribute>
    {
        protected override bool CanDrawAttributeProperty(InspectorProperty property)
        {
            return property.ValueEntry != null
                && typeof(GameData).IsAssignableFrom(property.ValueEntry.TypeOfValue);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var currentValue = Property.ValueEntry.WeakSmartValue as GameData;

            // 驗證當前選中的值是否含有指定的 DataFunction
            if (currentValue != null && Attribute.DataFunctionType != null)
            {
                if (!HasDataFunction(currentValue, Attribute.DataFunctionType))
                {
                    var warningMessage = $"選中的 GameData 不包含 {Attribute.DataFunctionType.Name}";
                    SirenixEditorGUI.WarningMessageBox(warningMessage);
                }
            }

            // 繪製帶過濾功能的選擇器
            DrawFilteredSelector(label, currentValue);

            GUI.backgroundColor = currentValue == null
                ? new Color(0.2f, 0.2f, 0.3f, 0.1f)
                : new Color(0.35f, 0.3f, 0.1f, 0.2f);

            var newObj = SirenixEditorFields.UnityObjectField(
                currentValue,
                Property.ValueEntry.TypeOfValue,
                false
            );
            Property.ValueEntry.WeakSmartValue = newObj;
            GUI.backgroundColor = Color.white;
        }

        private void DrawFilteredSelector(GUIContent label, GameData currentValue)
        {
            var buttonText = currentValue != null ? currentValue.name : "None";

            using (new GUILayout.HorizontalScope())
            {
                if (label != null)
                    EditorGUILayout.PrefixLabel(label);

                if (SirenixEditorGUI.SDFIconButton(
                    buttonText,
                    16,
                    SdfIconType.CaretDownFill,
                    IconAlignment.RightEdge))
                {
                    var selector = new DataFunctionFilteredSelector(Attribute.DataFunctionType);
                    selector.SelectionConfirmed += col =>
                    {
                        Property.ValueEntry.WeakSmartValue = col.FirstOrDefault();
                    };
                    selector.ShowInPopup();
                }
            }
        }

        private static bool HasDataFunction(GameData gameData, Type dataFunctionType)
        {
            return GameDataEditorUtility.HasDataFunction(gameData, dataFunctionType);
        }
    }

    /// <summary>
    /// DataFunction 過濾選擇器
    /// </summary>
    public class DataFunctionFilteredSelector : OdinSelector<GameData>
    {
        private readonly Type _dataFunctionType;

        public DataFunctionFilteredSelector(Type dataFunctionType)
        {
            _dataFunctionType = dataFunctionType;
            DrawConfirmSelectionButton = false;
            SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
            SelectionTree.Config.ConfirmSelectionOnDoubleClick = true;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;
            tree.Add("-- None --", null);

            var filteredOptions = GetFilteredGameData().ToList();

            if (!filteredOptions.Any())
            {
                tree.Add($"無含有 {_dataFunctionType?.Name ?? "指定 DataFunction"} 的 GameData", null);
                return;
            }

            // 按資料夾路徑分組
            var groupedOptions = filteredOptions
                .GroupBy(gd => GetAssetFolderName(gd))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in groupedOptions)
            {
                var sortedOptions = group.OrderBy(gd => gd.name).ToList();

                foreach (var gameData in sortedOptions)
                {
                    var path = string.IsNullOrEmpty(group.Key)
                        ? gameData.name
                        : $"{group.Key}/{gameData.name}";
                    tree.Add(path, gameData);
                }
            }
        }

        private IEnumerable<GameData> GetFilteredGameData()
        {
            return SOUtility.GetFilteredAssets<GameData>(
                "t:GameData",
                gd => GameDataEditorUtility.HasDataFunction(gd, _dataFunctionType)
            );
        }

        private static string GetAssetFolderName(GameData gameData)
        {
            var path = AssetDatabase.GetAssetPath(gameData);
            if (string.IsNullOrEmpty(path))
                return "";

            var folderPath = System.IO.Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folderPath))
                return "";

            return System.IO.Path.GetFileName(folderPath);
        }
    }

    /// <summary>
    /// GameData DataFunction 相關的編輯器工具方法
    /// </summary>
    public static class GameDataEditorUtility
    {
        /// <summary>
        /// 檢查 GameData 是否含有指定類型的 DataFunction
        /// </summary>
        public static bool HasDataFunction(GameData gameData, Type dataFunctionType)
        {
            if (gameData == null || dataFunctionType == null)
                return false;

            // 檢查 _dataFunctionList
            if (gameData.DataFunctions != null)
            {
                foreach (var df in gameData.DataFunctions)
                {
                    if (df != null && dataFunctionType.IsAssignableFrom(df.GetType()))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 取得所有含有指定 DataFunction 的 GameData
        /// </summary>
        public static List<GameData> FindGameDataWithDataFunction(Type dataFunctionType)
        {
            return SOUtility.GetFilteredAssets<GameData>(
                "t:GameData",
                gd => HasDataFunction(gd, dataFunctionType)
            );
        }

        /// <summary>
        /// 取得所有含有指定 DataFunction 的 GameData (泛型版本)
        /// </summary>
        public static List<GameData> FindGameDataWithDataFunction<T>() where T : AbstractDataFunction
        {
            return FindGameDataWithDataFunction(typeof(T));
        }
    }
}
