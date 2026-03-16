using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataType.Vector
{
    public class VarVector2Ref : AbstractValueSource<Vector2>, IVector2Provider,
        IValueSettable<Vector2>
    {
        protected override bool HasError()
        {
            if (_dropDownRef == GetComponentInParent<VarVector2>())
            {
                _errorMessage = "DropDownRef不能指向自己或父物件上的VarVector2";
                return true;
            }

            return base.HasError();
        }

        [Required] [DropDownRef] public VarVector2 _dropDownRef;

        public override Vector2 Value => _dropDownRef != null ? _dropDownRef.Value : Vector2.zero;

        public override string Description => _dropDownRef?.PathName;

        public void SetValue(Vector2 value, Object byWho = null, string reason = null)
        {
            _dropDownRef?.SetValue(value, byWho, reason);
        }
    }
}
