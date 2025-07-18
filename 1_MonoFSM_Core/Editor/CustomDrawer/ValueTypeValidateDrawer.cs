using JetBrains.Annotations;
using MonoFSM.Core;
using MonoFSM.Runtime.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// ValueTypeValidateAttribute 的 OdinAttributeDrawer
    /// 用於檢測和顯示 IValueProvider 的 ValueType 驗證結果
    /// </summary>
    [UsedImplicitly]
    [DrawerPriority(0, 1, 0)]
    public class ValueTypeValidateDrawer : OdinAttributeDrawer<ValueTypeValidateAttribute>
    {
        protected override bool CanDrawAttributeProperty(InspectorProperty property)
        {
            // 只處理有值的屬性
            return property.ValueEntry != null;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // 先繪製原本的屬性
            CallNextDrawer(label);
            
            // 檢查是否為 IValueProvider
            var provider = Property.ValueEntry?.WeakSmartValue as IValueProvider;
            if (provider == null)
            {
                // 如果欄位有值但不是 IValueProvider，顯示錯誤訊息
                if (Property.ValueEntry?.WeakSmartValue != null)
                {
                    SirenixEditorGUI.ErrorMessageBox("此欄位不是 IValueProvider 類型，無法進行 ValueType 驗證");
                }
                // 如果欄位為空，不顯示任何訊息
                return;
            }
            
            // 進行 ValueType 驗證
            var result = ValidateValueType(provider);
            
            // 根據驗證結果顯示對應的訊息
            if (!result.IsValid)
            {
                var errorMessage = result.ErrorMessage;
                if (result.Suggestions.Count > 0)
                {
                    errorMessage += "\n建議：" + string.Join("、", result.Suggestions);
                }
                SirenixEditorGUI.ErrorMessageBox(errorMessage);
            }
            else if (!string.IsNullOrEmpty(result.WarningMessage))
            {
                SirenixEditorGUI.WarningMessageBox(result.WarningMessage);
            }
            else if (Attribute.ShowSuccessMessage)
            {
                var successMessage = $"✓ ValueType 驗證成功：{provider.ValueType?.Name ?? "Unknown"}";
                SirenixEditorGUI.InfoMessageBox(successMessage);
            }
        }

        /// <summary>
        /// 驗證 ValueType 的輔助方法
        /// </summary>
        private TypeValidationResult ValidateValueType(IValueProvider provider)
        {
            if (provider == null)
            {
                return TypeValidationResult.Error("IValueProvider 為 null");
            }

            var actualType = provider.ValueType;
            if (actualType == null)
            {
                return TypeValidationResult.Error("IValueProvider.ValueType 為 null");
            }

            var expectedType = Attribute.ExpectedType;
            var allowCompatibleTypes = Attribute.AllowCompatibleTypes;
            var customErrorMessage = Attribute.CustomErrorMessage;

            // 完全相同的型別
            if (actualType == expectedType)
            {
                return TypeValidationResult.Success();
            }

            // 檢查是否允許相容型別
            if (allowCompatibleTypes)
            {
                if (TypeCompatibilityChecker.AreCompatible(actualType, expectedType))
                {
                    var score = TypeCompatibilityChecker.GetCompatibilityScore(actualType, expectedType);
                    if (score >= 80)
                    {
                        return TypeValidationResult.Success();
                    }
                    else if (score >= 60)
                    {
                        return TypeValidationResult.Warning(
                            $"型別相容但可能有精度損失：{actualType.Name} → {expectedType.Name}",
                            expectedType, actualType);
                    }
                }
            }

            // 型別不相容
            var errorMessage = !string.IsNullOrEmpty(customErrorMessage) 
                ? customErrorMessage 
                : $"ValueType 不符合期望。期望：{expectedType.Name}，實際：{actualType.Name}";

            var result = TypeValidationResult.Error(errorMessage, expectedType, actualType);
            result.Suggestions = TypeCompatibilityChecker.GetConversionSuggestions(actualType, expectedType);
            
            return result;
        }
    }
}