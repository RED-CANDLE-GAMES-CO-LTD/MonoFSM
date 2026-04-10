using System;
using MonoFSM.DataProvider;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class VarFloatRef : AbstractValueSource<float>, IFloatProvider, IFloatBoundProvider, IValueSettable<float>
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

        public string _previewName;
        public override string Description => _dropDownRef ? _dropDownRef.PathName : _previewName;

        public void SetValue(float value, UnityEngine.Object byWho = null, string reason = null)
        {
            this.Log("Set VarFloatRef Value: ", value);
            _dropDownRef?.SetValue(value, byWho, reason);
        }
    }
}
