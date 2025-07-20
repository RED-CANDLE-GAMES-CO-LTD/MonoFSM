using System;
using System.Collections.Generic;
using System.Linq;
using Auto.Utils;
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
                if (result.Suggestions != null && result.Suggestions.Count > 0)
                {
                    errorMessage += "\n建議：" + string.Join("、", result.Suggestions.ToArray());
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

            // 檢查是否使用多型別模式
            if (IsMultiTypeMode())
                return ValidateMultiType(actualType);
            else
                return ValidateSingleType(actualType);
        }

        /// <summary>
        /// 檢查是否使用多型別模式
        /// </summary>
        private bool IsMultiTypeMode()
        {
            return Attribute.ExpectedTypes != null || !string.IsNullOrEmpty(Attribute.DynamicTypePropertyName);
        }

        /// <summary>
        /// 驗證單一型別模式
        /// </summary>
        private TypeValidationResult ValidateSingleType(Type actualType)
        {
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

        /// <summary>
        /// 驗證多型別模式
        /// </summary>
        private TypeValidationResult ValidateMultiType(Type actualType)
        {
            // 獲取包含這個欄位的目標物件
            var target = Property.ParentValues.FirstOrDefault();
            if (target == null) return TypeValidationResult.Error("無法取得驗證目標物件");
            var expectedTypes = Attribute.GetExpectedTypes(target).ToList();

            // 獲取所有期望的型別
            // var expectedTypes = GetExpectedTypesFromAttribute(target).ToList();
            if (!expectedTypes.Any()) return TypeValidationResult.Error("沒有指定任何期望的型別");

            var allowCompatibleTypes = Attribute.AllowCompatibleTypes;
            var customErrorMessage = Attribute.CustomErrorMessage;

            // 檢查是否有完全匹配的型別
            if (expectedTypes.Contains(actualType)) return TypeValidationResult.Success();

            // 檢查是否允許相容型別
            if (allowCompatibleTypes)
            {
                var bestMatch = FindBestCompatibleType(actualType, expectedTypes);
                if (bestMatch != null)
                {
                    var score = TypeCompatibilityChecker.GetCompatibilityScore(actualType, bestMatch);
                    if (score >= 80)
                        return TypeValidationResult.Success();
                    else if (score >= 60)
                        return TypeValidationResult.Warning(
                            $"型別相容但可能有精度損失：{actualType.Name} → {bestMatch.Name}",
                            bestMatch, actualType);
                }
            }

            // 型別不相容
            var typeNames = expectedTypes.Select(t => t.Name).ToArray();
            var expectedTypeNames = string.Join("、", typeNames);
            var errorMessage = !string.IsNullOrEmpty(customErrorMessage)
                ? customErrorMessage
                : $"ValueType 不符合期望。期望：{expectedTypeNames}，實際：{actualType.Name}";

            var result = TypeValidationResult.Error(errorMessage, expectedTypes.First(), actualType);

            // 找到最佳的建議
            var allSuggestions = new List<string>();
            foreach (var expectedType in expectedTypes)
            {
                var suggestions = TypeCompatibilityChecker.GetConversionSuggestions(actualType, expectedType);
                if (suggestions != null) allSuggestions.AddRange(suggestions);
            }

            result.Suggestions = allSuggestions.Distinct().ToList();

            return result;
        }

        /// <summary>
        /// 從屬性獲取期望的型別
        /// </summary>
        // private IEnumerable<Type> GetExpectedTypesFromAttribute(object target)
        // {
        //     var result = new List<Type>();
        //
        //     // 靜態型別陣列模式
        //     if (Attribute.ExpectedTypes != null) result.AddRange(Attribute.ExpectedTypes);
        //
        //     // 動態型別模式
        //     if (!string.IsNullOrEmpty(Attribute.DynamicTypePropertyName) && target != null)
        //     {
        //         // 首先嘗試標準的方法查找（public 方法）
        //         var method = target.GetType().GetProperty(Attribute.DynamicTypePropertyName,
        //             System.Reflection.BindingFlags.Instance |
        //             System.Reflection.BindingFlags.Public);
        //
        //         // 如果找不到，使用 ReflectionHelperMethods 來查找繼承階層中的非公開方法
        //         if (method == null)
        //             method = ReflectionHelperMethods.GetNonPublicMethodInBaseClasses(target.GetType(),
        //                 Attribute.DynamicTypePropertyName);
        //
        //         if (method != null)
        //             try
        //             {
        //                 var methodResult = method.Invoke(target, null);
        //                 if (methodResult is IEnumerable<Type> types)
        //                     result.AddRange(types);
        //                 else if (methodResult is Type singleType) result.Add(singleType);
        //             }
        //             catch (Exception ex)
        //             {
        //                 Debug.LogError($"執行動態型別方法 '{Attribute.DynamicTypePropertyName}' 時發生錯誤: {ex.Message}");
        //             }
        //         else
        //             Debug.LogError($"找不到方法 '{Attribute.DynamicTypePropertyName}' 在型別 {target.GetType().Name} 中");
        //     }
        //
        //     return result;
        // }

        /// <summary>
        /// 在期望的型別中找到與實際型別最相容的型別
        /// </summary>
        private Type FindBestCompatibleType(Type actualType, IEnumerable<Type> expectedTypes)
        {
            Type bestMatch = null;
            var bestScore = 0;

            foreach (var expectedType in expectedTypes)
                if (TypeCompatibilityChecker.AreCompatible(actualType, expectedType))
                {
                    var score = TypeCompatibilityChecker.GetCompatibilityScore(actualType, expectedType);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = expectedType;
                    }
                }

            return bestMatch;
        }
    }
}