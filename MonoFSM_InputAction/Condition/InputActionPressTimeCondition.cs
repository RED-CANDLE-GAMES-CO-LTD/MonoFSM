using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_InputAction.Condition
{
    public class InputActionPressTimeCondition : AbstractConditionBehaviour
    {
        [DropDownRef]
        public MonoInputAction _inputAction;

        [ShowInInspector] private float pressingTime => _inputAction?.PressTime ?? 0;

        [InlineField]
        [SerializeField]
        VarFloatFoldOut _pressDuration = new VarFloatFoldOut();

        protected override bool IsValid =>
            _pressDuration.Value > 0 && _inputAction?.PressTime >= _pressDuration.Value;

        public override string Description =>
            _inputAction != null
                ? $"{_inputAction.name} Pressed for {_pressDuration.Value} seconds"
                : "No Input Action";
    }
}
