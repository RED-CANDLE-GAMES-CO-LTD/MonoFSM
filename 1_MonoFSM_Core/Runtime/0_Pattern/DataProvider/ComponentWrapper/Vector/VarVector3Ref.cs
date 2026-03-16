using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataType.Vector
{
    public class VarVector3Ref : AbstractValueSource<Vector3>, IVector3Provider,
        IValueSettable<Vector3>
    {
        protected override bool HasError()
        {
            if (_dropDownRef == GetComponentInParent<VarVector3>())
            {
                _errorMessage = "DropDownRef不能指向自己或父物件上的VarVector3";
                return true;
            }

            return base.HasError();
        }

        [Required] [DropDownRef] public VarVector3 _dropDownRef;

        public override Vector3 Value => _dropDownRef != null ? _dropDownRef.Value : Vector3.zero;

        public override string Description => _dropDownRef?.PathName;

        public void SetValue(Vector3 value, Object byWho = null, string reason = null)
        {
            _dropDownRef?.SetValue(value, byWho, reason);
        }
    }
}
