using System;
using System.Collections.Generic;
using System.Linq;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Editor.Utility;
using MonoFSM.CustomAttributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
// using MonoFSM.Core.Editor.Utility;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// TypeRestrictFilterAttribute 的通用 OdinAttributeDrawer
    /// 用於過濾帶有 RestrictType 屬性的欄位下拉選項
    /// 支援任何 ScriptableObject 類型
    /// </summary>
    public class SOTypeDropDownAttributeDrawer : OdinAttributeDrawer<SOTypeDropdownAttribute>
    {
        protected override bool CanDrawAttributeProperty(InspectorProperty property)
        {
            //FIXME: 這是對的嗎？
            return property.ValueEntry != null
                &&
                // (property.ValueEntry.TypeOfValue == typeof(VariableTag) ||
                //  property.ValueEntry.TypeOfValue == typeof(MonoEntityTag) ||
                typeof(ScriptableObject).IsAssignableFrom(property.ValueEntry.TypeOfValue);
        }

        /// <summary>
        ///     獲取期望的限制型別，如果 Attribute.RestrictInstanceType 為 null，則不進行限制型別過濾
        ///     優先使用 Attribute 指定的型別，若無則嘗試從 IConfigTypeProvider 取得
        /// </summary>
        private Type GetRestrictInstanceType()
        {
            // 優先使用 Attribute 明確指定的型別
            if (Attribute.RestrictInstanceType != null)
                return Attribute.RestrictInstanceType;

            // 嘗試從父物件的 IConfigTypeProvider 取得 RestrictType
            var parentObject = Property.ParentValues[0];
            if (parentObject is IConfigTypeProvider configTypeProvider)
            {
                var restrictType = configTypeProvider.GetRestrictType();
                // 只有當欄位類型和 RestrictType 相容時才使用
                // 例如：欄位是 GameData，RestrictType 是 RecipeData（GameData 的子類）=> 使用
                // 例如：欄位是 VariableTag，RestrictType 是 RecipeData => 不使用
                var fieldType = Property.ValueEntry.TypeOfValue;
                if (restrictType != null && fieldType.IsAssignableFrom(restrictType))
                {
                    return restrictType;
                }
            }

            return null;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var currentValue = Property.ValueEntry.WeakSmartValue as Object;
            var restrictInstanceType = GetRestrictInstanceType();

            // 驗證當前選中的值是否符合過濾條件
            if (currentValue != null && restrictInstanceType != null)
            {
                var actualRestrictType = Attribute.GetRestrictType(currentValue);
                var isValid = false;

                if (actualRestrictType != null)
                {
                    // 完全匹配
                    if (actualRestrictType == restrictInstanceType)
                        isValid = true;
                    // 檢查是否允許相容類型
                    else if (
                        Attribute.AllowCompatibleTypes
                        && restrictInstanceType.IsAssignableFrom(actualRestrictType)
                    )
                        isValid = true;
                }

                if (!isValid)
                {
                    // 顯示警告訊息
                    var expectedTypeName = restrictInstanceType.Name;
                    var actualTypeName = actualRestrictType?.Name ?? "Unknown";
                    var warningMessage = !string.IsNullOrEmpty(Attribute.CustomErrorMessage)
                        ? Attribute.CustomErrorMessage
                        : $"選中的限制型別不符合期望。期望：{expectedTypeName}，實際：{actualTypeName}";

                    SirenixEditorGUI.WarningMessageBox(warningMessage);
                }
            }

            // 繪製帶過濾功能的選擇器
            // var objValue = Property.ValueEntry.WeakSmartValue as Object;
            var totalRect = EditorGUILayout.BeginVertical();
            DrawFilteredSelector(label, currentValue);
            GUI.backgroundColor =
                Property.ValueEntry.WeakSmartValue == null
                    ? new Color(0.2f, 0.2f, 0.3f, 0.1f)
                    : new Color(0.35f, 0.3f, 0.1f, 0.2f);

            EditorGUI.BeginChangeCheck();
            var newObj = SirenixEditorFields.UnityObjectField(
                currentValue,
                Property.ValueEntry.TypeOfValue,
                false
            );
            if (EditorGUI.EndChangeCheck())
                Property.ValueEntry.WeakSmartValue = newObj;
            // CallNextDrawer(label);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            // 整個區塊（dropdown 按鈕列 + object field）都支援拖放
            HandleDragAndDrop(totalRect);
            //需要call next drawer嗎？
        }

        /// <summary>
        ///     處理整個欄位區塊的拖放，讓使用者把 Project 中的 SO 直接拖到欄位上
        /// </summary>
        private void HandleDragAndDrop(Rect dropArea)
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            var dragged = DragAndDrop.objectReferences;
            if (dragged == null || dragged.Length != 1)
                return;

            var draggedObj = dragged[0];
            var fieldType = Property.ValueEntry.TypeOfValue;
            if (!fieldType.IsInstanceOfType(draggedObj))
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Property.ValueEntry.WeakSmartValue = draggedObj;
                evt.Use();
                GUIHelper.RequestRepaint();
            }
        }

        private void DrawFilteredSelector(GUIContent label, Object currentValue)
        {
            // 繪製按鈕來開啟選擇器
            var buttonText = "None";

            //刪掉就會missing...
            if (currentValue != null && currentValue is ScriptableObject scriptableObj)
                buttonText = scriptableObj.name;

            using (new GUILayout.HorizontalScope())
            {
                if (label != null)
                    EditorGUILayout.PrefixLabel(label);

                if (
                    SirenixEditorGUI.SDFIconButton(
                        buttonText,
                        16,
                        SdfIconType.CaretDownFill,
                        IconAlignment.RightEdge
                    )
                )
                {
                    var selector = new TypeRestrictFilteredSelector(
                        Attribute,
                        Property.ValueEntry.TypeOfValue,
                        GetRestrictInstanceType()
                    );
                    selector.SelectionConfirmed += col =>
                    {
                        Property.ValueEntry.WeakSmartValue = col.FirstOrDefault();
                    };
                    selector.ShowInPopup();
                }
            }
        }
    }

    /// <summary>
    /// 通用的過濾選擇器
    /// </summary>
    public class TypeRestrictFilteredSelector : OdinSelector<ScriptableObject>
    {
        private readonly SOTypeDropdownAttribute _filterAttribute;
        private readonly Type _propertyType;
        private readonly Type _restrictInstanceType;

        public TypeRestrictFilteredSelector(
            SOTypeDropdownAttribute filterAttribute,
            Type propertyType,
            Type restrictInstanceType = null
        )
        {
            _filterAttribute = filterAttribute;
            _propertyType = propertyType;
            _restrictInstanceType = restrictInstanceType;
            DrawConfirmSelectionButton = false;
            SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
            SelectionTree.Config.ConfirmSelectionOnDoubleClick = true;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;

            // 添加 None 選項
            tree.Add("-- None --", null);

            // 獲取過濾後的選項
            var filteredOptions = GetFilteredOptions().ToList();

            if (!filteredOptions.Any())
            {
                tree.Add("無符合條件的選項", null);
                return;
            }

            // 按類型分組
            var groupedOptions = filteredOptions
                .Where(option => option != null)
                .GroupBy(option => GetGroupName(option))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in groupedOptions)
            {
                var sortedOptions = group.OrderBy(option => option.name).ToList();

                foreach (var option in sortedOptions)
                {
                    var displayName = $"{option.name}";
                    var path = group.Key == "Unknown" ? displayName : $"{group.Key}/{displayName}";

                    tree.Add(path, option);
                }
            }
        }

        private IEnumerable<ScriptableObject> GetFilteredOptions()
        {
            var searchFilter = _filterAttribute.GetAssetSearchFilter(_propertyType);
            return SOUtility.GetFilteredAssets<ScriptableObject>(searchFilter, ValidateAsset);
        }

        private bool ValidateAsset(ScriptableObject asset)
        {
            if (asset == null)
                return false;

            // 首先檢查 Property Type 是否匹配
            if (!_propertyType.IsAssignableFrom(asset.GetType()))
                return false;

            // 如果沒有指定 RestrictInstanceType，則只要 Property Type 匹配就接受
            if (_restrictInstanceType == null)
                return true;

            // 檢查 Restrict Type 是否匹配
            var actualRestrictType = _filterAttribute.GetRestrictType(asset);
            if (actualRestrictType == null)
                return false;

            // 完全匹配
            if (actualRestrictType == _restrictInstanceType)
                return true;

            // 檢查是否允許相容類型
            if (_filterAttribute.AllowCompatibleTypes)
                return _restrictInstanceType.IsAssignableFrom(actualRestrictType);

            return false;
        }

        private string GetGroupName(ScriptableObject asset)
        {
            if (asset == null)
                return "Unknown";

            var restrictType = _filterAttribute.GetRestrictType(asset);
            return restrictType?.Name ?? asset.GetType().Name;
        }
    }
}
