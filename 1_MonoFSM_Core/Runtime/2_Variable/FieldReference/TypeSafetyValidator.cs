using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// 型別驗證結果
    /// </summary>
    [Serializable]
    public class TypeValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public string WarningMessage;
        public Type ExpectedType;
        public Type ActualType;
        public int StepIndex;
        public List<string> Suggestions = new();

        public TypeValidationResult(bool isValid = true)
        {
            IsValid = isValid;
        }

        public static TypeValidationResult Success()
        {
            return new TypeValidationResult(true);
        }

        public static TypeValidationResult Error(string message, Type expected = null, Type actual = null,
            int stepIndex = -1)
        {
            return new TypeValidationResult(false)
            {
                ErrorMessage = message,
                ExpectedType = expected,
                ActualType = actual,
                StepIndex = stepIndex
            };
        }

        public static TypeValidationResult Warning(string message, Type expected = null, Type actual = null)
        {
            return new TypeValidationResult(true)
            {
                WarningMessage = message,
                ExpectedType = expected,
                ActualType = actual
            };
        }
    }

    /// <summary>
    /// 型別相容性檢查器
    /// </summary>
    public static class TypeCompatibilityChecker
    {
        /// <summary>
        /// 檢查兩個型別是否相容
        /// </summary>
        public static bool AreCompatible(Type sourceType, Type targetType)
        {
            if (sourceType == null || targetType == null)
                return false;

            // 完全相同
            if (sourceType == targetType)
                return true;

            // 可指派性檢查
            if (targetType.IsAssignableFrom(sourceType))
                return true;

            // 基本型別轉換
            if (IsNumericType(sourceType) && IsNumericType(targetType))
                return true;

            // 字串轉換
            if (targetType == typeof(string))
                return true;

            return false;
        }

        /// <summary>
        /// 取得型別相容性分數（0-100，100表示完全相容）
        /// </summary>
        public static int GetCompatibilityScore(Type sourceType, Type targetType)
        {
            if (sourceType == null || targetType == null)
                return 0;

            // 完全相同
            if (sourceType == targetType)
                return 100;

            // 可直接指派
            if (targetType.IsAssignableFrom(sourceType))
                return 90;

            // 數值型別之間的轉換
            if (IsNumericType(sourceType) && IsNumericType(targetType))
            {
                // 無損轉換
                if (CanConvertWithoutLoss(sourceType, targetType))
                    return 80;
                // 可能有損轉換
                return 60;
            }

            // 字串轉換
            if (targetType == typeof(string))
                return 50;

            // IConvertible 型別
            if (typeof(IConvertible).IsAssignableFrom(sourceType) &&
                typeof(IConvertible).IsAssignableFrom(targetType))
                return 40;

            return 0;
        }

        /// <summary>
        /// 是否為數值型別
        /// </summary>
        public static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        /// <summary>
        /// 檢查是否可以無損轉換
        /// </summary>
        public static bool CanConvertWithoutLoss(Type fromType, Type toType)
        {
            var numericHierarchy = new[]
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };

            var fromIndex = Array.IndexOf(numericHierarchy, fromType);
            var toIndex = Array.IndexOf(numericHierarchy, toType);

            if (fromIndex == -1 || toIndex == -1)
                return false;

            // 簡化的無損轉換檢查
            return fromIndex <= toIndex;
        }

        /// <summary>
        /// 取得型別轉換建議
        /// </summary>
        public static List<string> GetConversionSuggestions(Type sourceType, Type targetType)
        {
            var suggestions = new List<string>();

            if (sourceType == null || targetType == null)
                return suggestions;

            if (AreCompatible(sourceType, targetType))
            {
                suggestions.Add("型別相容，可直接使用");
                return suggestions;
            }

            // 數值型別建議
            if (IsNumericType(sourceType) && IsNumericType(targetType))
            {
                if (CanConvertWithoutLoss(sourceType, targetType))
                    suggestions.Add("可進行無損數值轉換");
                else
                    suggestions.Add("可進行數值轉換，但可能失去精度");
            }

            // 字串轉換建議
            if (targetType == typeof(string))
                suggestions.Add("可使用 ToString() 轉換為字串");

            // 基底類別建議
            if (sourceType.BaseType != null && targetType.IsAssignableFrom(sourceType.BaseType))
                suggestions.Add($"可考慮使用基底類別 {sourceType.BaseType.Name}");

            // 介面建議
            var commonInterfaces = sourceType.GetInterfaces().Intersect(targetType.GetInterfaces());
            foreach (var iface in commonInterfaces.Take(3)) suggestions.Add($"可考慮使用共同介面 {iface.Name}");

            if (suggestions.Count == 0)
                suggestions.Add("型別不相容，需要自訂轉換邏輯");

            return suggestions;
        }
    }

    /// <summary>
    /// 型別安全驗證器
    /// </summary>
    public static class TypeSafetyValidator
    {
        /// <summary>
        /// 驗證 ValueAccessChain 的型別安全性
        /// </summary>
        public static TypeValidationResult ValidateAccessChain(ValueAccessChain accessChain)
        {
            if (accessChain == null)
                return TypeValidationResult.Error("存取鏈為 null");

            if (accessChain.VariableTag == null)
                return TypeValidationResult.Error("變數標籤未設定");

            var steps = accessChain.AccessSteps;
            if (steps.Count == 0)
                // 沒有步驟，直接返回變數值，這是有效的
                return TypeValidationResult.Success();

            var currentType = accessChain.VariableTag.ValueType;
            if (currentType == null)
                return TypeValidationResult.Error("無法確定變數的值型別");

            // 逐步驗證每個存取步驟
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var result = ValidateAccessStep(step, currentType, i);

                if (!result.IsValid)
                    return result;

                // 更新當前型別為此步驟的輸出型別
                currentType = step.StepOutputType;
                if (currentType == null)
                    return TypeValidationResult.Error($"步驟 {i + 1} 的輸出型別無法確定",
                        stepIndex: i);
            }

            return TypeValidationResult.Success();
        }

        /// <summary>
        /// 驗證單個存取步驟
        /// </summary>
        public static TypeValidationResult ValidateAccessStep(AccessStep step, Type inputType, int stepIndex)
        {
            if (step == null)
                return TypeValidationResult.Error($"步驟 {stepIndex + 1} 為 null", stepIndex: stepIndex);

            if (step.fieldReference == null)
                return TypeValidationResult.Error($"步驟 {stepIndex + 1} 的欄位引用未設定", stepIndex: stepIndex);

            var fieldRef = step.fieldReference;

            // 驗證欄位引用本身
            if (!fieldRef.ValidateReference())
                return TypeValidationResult.Error($"步驟 {stepIndex + 1} 的欄位引用無效", stepIndex: stepIndex);

            // 檢查輸入型別相容性
            var expectedInputType = fieldRef.SourceType;
            if (expectedInputType != null && inputType != null)
                if (!TypeCompatibilityChecker.AreCompatible(inputType, expectedInputType))
                {
                    var suggestions = TypeCompatibilityChecker.GetConversionSuggestions(inputType, expectedInputType);
                    return TypeValidationResult.Error(
                        $"步驟 {stepIndex + 1} 的輸入型別不相容。期望: {expectedInputType.Name}, 實際: {inputType.Name}",
                        expectedInputType, inputType, stepIndex);
                }

            // 檢查陣列存取
            if (step.IsArrayField)
            {
                if (!fieldRef.IsArray)
                    return TypeValidationResult.Error($"步驟 {stepIndex + 1} 嘗試對非陣列欄位進行陣列存取", stepIndex: stepIndex);

                if (step.arrayIndex < 0)
                    return TypeValidationResult.Error($"步驟 {stepIndex + 1} 的陣列索引不能為負數", stepIndex: stepIndex);

                // 注意：我們無法在編輯時驗證陣列範圍，只能在執行時檢查
            }

            return TypeValidationResult.Success();
        }

        /// <summary>
        /// 驗證 FieldReference 的型別安全性
        /// </summary>
        public static TypeValidationResult ValidateFieldReference(FieldReference fieldRef)
        {
            if (fieldRef == null)
                return TypeValidationResult.Error("欄位引用為 null");

            if (fieldRef.SourceType == null)
                return TypeValidationResult.Error("來源型別未設定");

            if (string.IsNullOrEmpty(fieldRef.FieldName))
                return TypeValidationResult.Error("欄位名稱未設定");

            if (!fieldRef.ValidateReference())
                return TypeValidationResult.Error("欄位引用驗證失敗");

            if (fieldRef.FieldType == null)
                return TypeValidationResult.Warning("無法確定欄位型別");

            return TypeValidationResult.Success();
        }

        /// <summary>
        /// 檢查 ChainedValueProvider 的型別安全性
        /// </summary>
        public static TypeValidationResult ValidateChainedValueProvider(ChainedValueProvider provider)
        {
            if (provider == null)
                return TypeValidationResult.Error("鏈式值提供者為 null");

            // 檢查內部設定
            if (!provider.ValidateSetup())
                return TypeValidationResult.Error($"鏈式值提供者設定無效: {provider.LastError}");

            return TypeValidationResult.Success();
        }

        /// <summary>
        /// 執行完整的型別安全性分析
        /// </summary>
        public static List<TypeValidationResult> PerformFullAnalysis(ValueAccessChain accessChain)
        {
            var results = new List<TypeValidationResult>();

            // 基本驗證
            var basicResult = ValidateAccessChain(accessChain);
            results.Add(basicResult);

            if (!basicResult.IsValid)
                return results;

            // 詳細步驟分析
            if (accessChain.AccessSteps.Count > 0)
            {
                var currentType = accessChain.VariableTag.ValueType;

                for (var i = 0; i < accessChain.AccessSteps.Count; i++)
                {
                    var step = accessChain.AccessSteps[i];

                    // 驗證欄位引用
                    var fieldResult = ValidateFieldReference(step.fieldReference);
                    fieldResult.StepIndex = i;
                    results.Add(fieldResult);

                    // 驗證步驟
                    var stepResult = ValidateAccessStep(step, currentType, i);
                    results.Add(stepResult);

                    if (stepResult.IsValid)
                        currentType = step.StepOutputType;
                }
            }

            // 效能分析建議
            var performanceResult = AnalyzePerformance(accessChain);
            if (!string.IsNullOrEmpty(performanceResult.WarningMessage))
                results.Add(performanceResult);

            return results;
        }

        /// <summary>
        /// 分析效能並提供建議
        /// </summary>
        private static TypeValidationResult AnalyzePerformance(ValueAccessChain accessChain)
        {
            var result = TypeValidationResult.Success();
            var suggestions = new List<string>();

            if (accessChain.AccessSteps.Count > 5) suggestions.Add("存取鏈步驟較多，考慮啟用快取來提升效能");

            var hasArrayAccess = accessChain.AccessSteps.Any(s => s.IsArrayField);
            if (hasArrayAccess) suggestions.Add("包含陣列存取，注意檢查陣列邊界");

            var hasReflection = accessChain.AccessSteps.Any(s => s.fieldReference != null);
            if (hasReflection) suggestions.Add("使用反射存取，在效能敏感的地方考慮快取結果");

            if (suggestions.Count > 0)
            {
                result.WarningMessage = "效能建議";
                result.Suggestions = suggestions;
            }

            return result;
        }

        /// <summary>
        /// 取得型別安全性總結報告
        /// </summary>
        public static string GenerateTypeSafetyReport(ValueAccessChain accessChain)
        {
            var results = PerformFullAnalysis(accessChain);
            var report = "=== 型別安全性分析報告 ===\n\n";

            var errors = results.Where(r => !r.IsValid).ToList();
            var warnings = results.Where(r => r.IsValid && !string.IsNullOrEmpty(r.WarningMessage)).ToList();

            report += $"總體狀態: {(errors.Count == 0 ? "✓ 型別安全" : "✗ 發現錯誤")}\n";
            report += $"錯誤數量: {errors.Count}\n";
            report += $"警告數量: {warnings.Count}\n\n";

            if (errors.Count > 0)
            {
                report += "錯誤列表:\n";
                foreach (var error in errors)
                {
                    report += $"- {error.ErrorMessage}\n";
                    if (error.Suggestions.Count > 0) report += "  建議: " + string.Join("; ", error.Suggestions) + "\n";
                }

                report += "\n";
            }

            if (warnings.Count > 0)
            {
                report += "警告列表:\n";
                foreach (var warning in warnings)
                {
                    report += $"- {warning.WarningMessage}\n";
                    if (warning.Suggestions.Count > 0)
                        report += "  建議: " + string.Join("; ", warning.Suggestions) + "\n";
                }

                report += "\n";
            }

            if (errors.Count == 0 && warnings.Count == 0) report += "所有檢查都通過，存取鏈型別安全！\n";

            return report;
        }
    }
}