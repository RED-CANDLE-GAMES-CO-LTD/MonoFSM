using MonoFSM.Variable;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 從任意 Component 的 bool property 取值的 ValueSource
    /// </summary>
    public class BoolComponentPropertyValueSource
        : AbstractComponentPropertyValueSource<bool>, IBoolProvider
    {
        public bool IsTrue => Value;
    }
}
