using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper.Float
{
    /// <summary>
    /// 回傳 VarFloat 的百分比值 (CurrentValue - Min) / (Max - Min)，範圍 0~1
    /// </summary>
    public class FloatPercentage : AbstractValueSource<float>, IFloatProvider
    {
        protected override string DescriptionTag => "%";

        [Required] [DropDownRef] public VarFloat _varFloat;

        public override float Value => _varFloat != null ? _varFloat.Percentage : 0f;

        public override string Description => " of: " + _varFloat?.Description;
    }
}
