using System;
using System.Diagnostics;

namespace MonoFSM.Foundation
{
    /// <summary>
    ///     標記「Var 快速建立」(選中 Var 節點按 Alt+V) 選單裡的常用選項，會被排到最上面的置頂區。
    ///     兩種用法：
    ///     1. 標在 class 上 —— 這個型別置頂，Priority 大的在前
    ///     <code>
    /// [QuickCreate(Priority = 90)]
    /// public class VarFloatCompareConstCondition : AbstractConditionBehaviour
    /// </code>
    ///     2. 標在 static void Xxx(TSelf comp) 上 —— 置頂並在建立後預填欄位，一個 method 一個選項
    ///     <code>
    /// [QuickCreate("Float &gt; 0", Priority = 95)]
    /// private static void Preset_GreaterThanZero(VarFloatCompareConstCondition c)
    /// {
    ///     c._op = Operator.GreaterThan;
    ///     c._targetValue = 0f;
    /// }
    /// </code>
    ///     Condition 也可以改用既有的 [ConditionPreset]，那個同時會出現在 Inspector 的 PresetBar；
    ///     這個 attribute 的差別是 Action / Getter 也能用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class QuickCreateAttribute : Attribute
    {
        public QuickCreateAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }

        /// <summary>選單上的顯示名，留空用型別名</summary>
        public string DisplayName;

        /// <summary>越大越前面</summary>
        public int Priority;

        /// <summary>
        ///     指定要回填 Var 的欄位名，可用 "_wrapper._var" 這種一層巢狀路徑。
        ///     留空的話自動挑（精確型別 &gt; Required &gt; DropDownRef &gt; 宣告順序）。
        /// </summary>
        public string FieldName;
    }
}
