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

        [Tooltip("選擇要回傳 VarFloat 的哪種數值")]
        public VarFloatValueType _valueType = VarFloatValueType.CurrentValue;

        public override float Value => _dropDownRef != null ? GetValueByType() : 0f;

        private float GetValueByType()
        {
            switch (_valueType)
            {
                case VarFloatValueType.Min:
                    return _dropDownRef.Min;
                case VarFloatValueType.Max:
                    return _dropDownRef.Max;
                case VarFloatValueType.Percentage:
                    return _dropDownRef.Percentage;
                case VarFloatValueType.CurrentValue:
                default:
                    return _dropDownRef.Value;
            }
        }

        public Type ValueType => typeof(float);

        public float Min => _dropDownRef != null ? _dropDownRef.Min : float.MinValue;
        public float Max => _dropDownRef != null ? _dropDownRef.Max : float.MaxValue;

        public string _previewName;

        public override string Description =>
            _dropDownRef ? $"{_dropDownRef.PathName} ({_valueType})" : _previewName;

        private bool ShowWarning() => _valueType != VarFloatValueType.CurrentValue;

        [InfoBox("注意: 透過變數寫入數值時, 永遠只會寫入到 CurrentValue, 不受選擇的 \'_valueType\' 影響",
            InfoMessageType.Warning, "ShowWarning")]
        public void SetValue(float value, UnityEngine.Object byWho = null, string reason = null)
        {
            this.Log("Set VarFloatRef Value: ", value);
            _dropDownRef?.SetValue(value, byWho, reason);
        }
    }

    public enum VarFloatValueType
    {
        CurrentValue,
        Min,
        Max,
        Percentage
    }
}
