using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class VarStatBaseValueRef : AbstractValueSource<float>, IFloatProvider,
        IFloatBoundProvider
    {
        [Required] [DropDownRef] public VarStat _dropDownRef;

        public override float Value => _dropDownRef != null ? _dropDownRef.Field.CurrentValue : 0f;

        public float Min => _dropDownRef != null ? _dropDownRef.Min : 0f;
        public float Max => _dropDownRef != null ? _dropDownRef.Max : float.MaxValue;

        public override string Description => "BaseValue of " + _dropDownRef?.PathName;
    }
}
