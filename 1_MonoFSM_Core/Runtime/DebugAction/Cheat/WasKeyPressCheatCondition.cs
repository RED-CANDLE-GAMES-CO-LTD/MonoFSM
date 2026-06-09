using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    /// <summary>
    ///  Condition that checks if a specific key is pressed.
    ///  FIXME: 應該要 Debug mode才？
    /// </summary>
    public class WasKeyPressCheatCondition : AbstractConditionBehaviour //FIXME: parent的模組需要拔掉的話怎麼辦？
    {
        // [Obsolete]
        // [SerializeField]
        // private KeyCode _keyCode;
        public override string Description =>
            _isPress ? $"Is Key Pressed: {_key}" : $"Was Key Pressed: {_key}";

        [SerializeField]
        private Key _key;

        [SerializeField]
        [Tooltip("勾選：持續按住 (isPressed)；不勾：這一幀按下 (wasPressedThisFrame)")]
        private bool _isPress;

        // [CompRef]
        // [AutoParent]
        // private IConditionChangeListener _parentConditionChangeListener;

        // private bool _lastIsValid = false;
        protected override bool IsValid =>
            _key > 0
            && (_isPress
                ? Keyboard.current[_key].isPressed
                : Keyboard.current[_key].wasPressedThisFrame);

        //VarStat應該不會update...怎麼監聽？需要update? IConditionUpdater?
        // private void Update()
        // {
        //     if (IsValid == _lastIsValid)
        //         return;
        //     // Debug.Log($"Cheat Condition Activated: {_keyCode} {IsValid}", this);
        //     // _parentConditionChangeListener.OnConditionChanged();
        //     _lastIsValid = IsValid;
        // }
    }
}
