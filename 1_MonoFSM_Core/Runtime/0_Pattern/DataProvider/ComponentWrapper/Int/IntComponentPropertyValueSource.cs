namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 從任意 Component 的 int property 取值的 ValueSource
    /// </summary>
    public class IntComponentPropertyValueSource
        : AbstractComponentPropertyValueSource<int>, IIntProvider
    {
        public int IntValue => Value;
    }
}
