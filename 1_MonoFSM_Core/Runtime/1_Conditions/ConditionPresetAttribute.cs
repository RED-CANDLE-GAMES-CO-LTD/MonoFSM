using System;

namespace MonoFSM.Condition
{
    /// <summary>
    /// 標在 AbstractConditionBehaviour 子類的 static method 上，
    /// 由 CompRef 的 PresetBar 在 Inspector 上聚合為「一鍵加入並預填」按鈕。
    /// Method 必須是 static，且接受單一參數為對應的 Condition 型別 (或其父類)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ConditionPresetAttribute : Attribute
    {
        public string DisplayName;
        public string Category;
        public int Priority;
        public string ColorHex;

        public ConditionPresetAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}
