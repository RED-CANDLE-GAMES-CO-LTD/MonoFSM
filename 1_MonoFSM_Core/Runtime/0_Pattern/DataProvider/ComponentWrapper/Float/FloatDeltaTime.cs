using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Runtime;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper.Float
{
    /// <summary>
    /// 大多情況用 FloatChangePerSecondAction 就好？
    /// </summary>
    public class FloatDeltaTime : AbstractValueSource<float>, IFloatProvider
    {
        protected override string DescriptionTag => "Float";
        public override float Value => WorldUpdateSimulator.DeltaTime;
    }
}
