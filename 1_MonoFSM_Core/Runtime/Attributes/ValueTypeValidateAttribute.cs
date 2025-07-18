using System;
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
        /// 期望的 ValueType
        /// </summary>
        public Type ExpectedType { get; }

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
        /// 建構函式
        /// </summary>
        /// <param name="expectedType">期望的 ValueType</param>
        public ValueTypeValidateAttribute(Type expectedType)
        {
            ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
        }
    }
}