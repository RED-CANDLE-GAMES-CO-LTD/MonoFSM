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
        public override string Description =>
            _isPress ? $"Is Key Pressed: {_key}" : $"Was Key Pressed: {_key}";

        [SerializeField]
        private Key _key;

        [SerializeField]
        [Tooltip("勾選：持續按住 (isPressed)；不勾：這一幀按下 (wasPressedThisFrame)")]
        private bool _isPress;

        [SerializeField]
        [Tooltip("勾選：若有 Ctrl/Alt/Shift/Cmd 任一 modifier 鍵被按住，就不觸發（避免組合鍵誤觸）")]
        private bool _ignoreIfModifierHeld;

        private bool IsModifierHeld =>
            Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.altKey.isPressed
            || Keyboard.current.shiftKey.isPressed
            || Keyboard.current.leftMetaKey.isPressed
            || Keyboard.current.rightMetaKey.isPressed;

        protected override bool IsValid =>
            _key > 0
            && !(_ignoreIfModifierHeld && IsModifierHeld)
            && (_isPress
                ? Keyboard.current[_key].isPressed
                : Keyboard.current[_key].wasPressedThisFrame);
    }
}
