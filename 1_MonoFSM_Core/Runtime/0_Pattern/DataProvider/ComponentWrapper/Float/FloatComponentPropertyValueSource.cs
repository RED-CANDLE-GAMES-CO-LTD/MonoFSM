using MonoFSM.Core.DataProvider;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 從任意 Component 的 float property 取值的 ValueSource
    /// </summary>
    public class FloatComponentPropertyValueSource
        : AbstractComponentPropertyValueSource<float>, IFloatProvider
    {
    }
}
