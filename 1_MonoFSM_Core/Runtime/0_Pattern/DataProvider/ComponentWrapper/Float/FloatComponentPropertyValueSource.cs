using System;
using MonoFSM.Core.DataProvider;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 從任意 Component 的 float property 取值的 ValueSource
    /// </summary>
    [Obsolete] //FIXME: 還是不要走reflection吧
    public class FloatComponentPropertyValueSource
        : AbstractComponentPropertyValueSource<float>, IFloatProvider
    {
    }
}
