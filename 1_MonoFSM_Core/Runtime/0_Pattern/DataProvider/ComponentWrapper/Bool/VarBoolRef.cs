using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    //FIXME: 好像不如用 condition? 這個composite condition會不能用耶
    public class VarBoolRef : AbstractValueSource<bool>, IBoolProvider, IValueSettable<bool>
    {
        protected override bool HasError()
        {
            if (_dropDownRef == GetComponentInParent<VarBool>())
            {
                _errorMessage = "DropDownRef不能指向自己或父物件上的VarBool";
                return true;
            }

            return base.HasError();
        }

        [Required] [DropDownRef] public VarBool _dropDownRef;

        public override bool Value => _dropDownRef != null && _dropDownRef.Value;
        public bool IsTrue => Value;

        //TODO: hover可以看到 path Name啦
        public override string Description => _dropDownRef?.Description;

        public void SetValue(bool value, Object byWho = null, string reason = null)
        {
            _dropDownRef?.SetValue(value, byWho, reason);
        }
    }
}
