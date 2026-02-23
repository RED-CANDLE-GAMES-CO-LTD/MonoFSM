using System;
using MonoFSM.DataProvider;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    //FIXME: 這不能 set 呀？
    public class VarFloatRef : AbstractValueSource<float>, IFloatProvider, IFloatBoundProvider
    {
        protected override bool HasError()
        {
            if (_dropDownRef == GetComponentInParent<VarFloat>())
            {
                _errorMessage = "DropDownRef不能指向自己或父物件上的VarFloat";
                return true;
            }

            return base.HasError();
        }

        [Required] [DropDownRef] public VarFloat _dropDownRef;

        public override float Value => _dropDownRef != null ? _dropDownRef.Value : 0f;
        public Type ValueType => typeof(float);

        public float Min => _dropDownRef != null ? _dropDownRef.Min : float.MinValue;
        public float Max => _dropDownRef != null ? _dropDownRef.Max : float.MaxValue;

        public override string Description => "DropDownRef: " + _dropDownRef?.Description;
    }
}
