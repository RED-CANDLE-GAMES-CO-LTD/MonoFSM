using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    //FIXME: 用起來有點瑣碎...還是可以怎麼在FloatPercentage那裡去想辦法在調用Max時選擇BaseValue or CurrentValue (這樣Max就不能是單純的Float了)
    public class VarStatBaseValueRef : AbstractValueSource<float>, IFloatProvider,
        IFloatBoundProvider
    {
        [Required] [DropDownRef] public VarStat _dropDownRef;

        public override float Value =>
            _dropDownRef != null ? _dropDownRef.Field.CurrentValue : 0f; //是 current不是 Final!

        public float Min => _dropDownRef != null ? _dropDownRef.Min : 0f;
        public float Max => _dropDownRef != null ? _dropDownRef.Max : float.MaxValue;

        public override string Description =>
            _dropDownRef?.Name + ".BaseValue"; //FIXME: 不是baseValue啊？
    }
}
