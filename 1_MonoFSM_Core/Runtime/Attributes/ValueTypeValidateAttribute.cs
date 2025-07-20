using System;
using System.Collections.Generic;
using Auto.Utils;
using Sirenix.OdinInspector;

namespace MonoFSM.Runtime.Attributes
{
    /// <summary>
    /// 驗證 IValueProvider 欄位的 ValueType 是否符合期望的型別
    /// 使用 Odin Inspector 的 InfoBox 顯示驗證結果
    /// 
    /// 使用方式：
    /// [ValueTypeValidate(typeof(float))]
    /// public IValueProvider floatProvider;
    /// 
    /// 需要配合條件式 InfoBox 使用：
    /// [InfoBox("型別不符合期望", InfoMessageType.Error, "IsValueTypeInvalid")]
    /// [ValueTypeValidate(typeof(float))]
    /// public IValueProvider floatProvider;
    /// 
    /// 或者使用 ValueTypeValidateWithInfoBox 複合屬性。
    /// </summary>
    [IncludeMyAttributes]
    [AttributeUsage(AttributeTargets.Field)]
    public class ValueTypeValidateAttribute : Attribute
    {
        /// <summary>
        /// 期望的 ValueType（單一型別模式）
        /// </summary>
        public Type ExpectedType { get; }

        /// <summary>
        /// 期望的 ValueType 陣列（多型別模式）
        /// </summary>
        public Type[] ExpectedTypes { get; }

        /// <summary>
        /// 動態型別獲取函式名稱
        /// </summary>
        public string DynamicTypePropertyName { get; }

        /// <summary>
        /// 是否允許相容型別（預設為 true）
        /// </summary>
        public bool AllowCompatibleTypes { get; set; } = true;

        /// <summary>
        /// 是否在驗證成功時顯示提示（預設為 false）
        /// </summary>
        public bool ShowSuccessMessage { get; set; } = false;

        /// <summary>
        /// 自訂錯誤訊息
        /// </summary>
        public string CustomErrorMessage { get; set; }

        /// <summary>
        /// 建構函式 - 單一型別
        /// </summary>
        /// <param name="expectedType">期望的 ValueType</param>
        public ValueTypeValidateAttribute(Type expectedType)
        {
            ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
        }

        /// <summary>
        /// 建構函式 - 多型別陣列
        /// </summary>
        /// <param name="expectedTypes">期望的 ValueType 陣列</param>
        public ValueTypeValidateAttribute(params Type[] expectedTypes)
        {
            ExpectedTypes = expectedTypes ?? throw new ArgumentNullException(nameof(expectedTypes));
        }

        /// <summary>
        /// 建構函式 - 動態函式
        /// </summary>
        /// <param name="dynamicTypeMethodName">返回 Type[] 或 IEnumerable&lt;Type&gt; 的方法名稱</param>
        public ValueTypeValidateAttribute(string dynamicTypeMethodName)
        {
            DynamicTypePropertyName =
                dynamicTypeMethodName ?? throw new ArgumentNullException(nameof(dynamicTypeMethodName));
        }

        /// <summary>
        /// 建構函式 - 混合使用動態函式和靜態型別
        /// </summary>
        /// <param name="dynamicTypeMethodName">返回 Type[] 或 IEnumerable&lt;Type&gt; 的方法名稱</param>
        /// <param name="additionalTypes">額外的靜態型別</param>
        // public ValueTypeValidateAttribute(string dynamicTypeMethodName, params Type[] additionalTypes)
        // {
        //     DynamicTypeMethodName =
        //         dynamicTypeMethodName ?? throw new ArgumentNullException(nameof(dynamicTypeMethodName));
        //     ExpectedTypes = additionalTypes ?? Array.Empty<Type>();
        // }

        /// <summary>
        /// 獲取所有期望的型別（包含靜態和動態）
        /// </summary>
        /// <param name="target">包含方法的目標物件</param>
        /// <returns>所有期望的型別</returns>
        public IEnumerable<Type> GetExpectedTypes(object target)
        {
            var result = new List<Type>();

            // 單一型別模式
            if (ExpectedType != null) result.Add(ExpectedType);

            // 多型別陣列模式
            if (ExpectedTypes != null) result.AddRange(ExpectedTypes);

            // 動態型別模式
            if (!string.IsNullOrEmpty(DynamicTypePropertyName) && target != null)
            {
                // 首先嘗試標準的方法查找（public 方法）
                var method = target.GetType().GetProperty(DynamicTypePropertyName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);

                // 如果找不到，使用 ReflectionHelperMethods 來查找繼承階層中的非公開方法
                if (method == null)
                    method = ReflectionHelperMethods.GetNonPublicPropertyInBaseClasses(target.GetType(),
                        DynamicTypePropertyName);

                if (method != null)
                    try
                    {
                        var methodResult = method.GetMethod.Invoke(target, null);
                        if (methodResult is IEnumerable<Type> types)
                            result.AddRange(types);
                        else if (methodResult is Type singleType) result.Add(singleType);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"執行動態型別方法 '{DynamicTypePropertyName}' 時發生錯誤: {ex.Message}");
                    }
                else
                    UnityEngine.Debug.LogWarning($"找不到方法 '{DynamicTypePropertyName}' 在型別 {target.GetType().Name} 中");
            }

            return result;
        }

        /// <summary>
        /// 檢查是否使用多型別模式
        /// </summary>
        public bool IsMultiTypeMode => ExpectedTypes != null || !string.IsNullOrEmpty(DynamicTypePropertyName);
    }
}