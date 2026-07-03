using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RCGInputAction
{
    public enum InputSchemeType
    {
        KeyboardMouse,
        Gamepad,
    }

    //取代已移除的 PlayerInputBinder：用最後觸發 action 的裝置判斷目前輸入方式
    public static class InputSchemeWatcher
    {
        public static InputSchemeType CurrentScheme { get; private set; } =
            InputSchemeType.KeyboardMouse;

        public static event Action<InputSchemeType> OnSchemeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            //domain reload 關閉時避免重複註冊
            InputSystem.onActionChange -= HandleActionChange;
            InputSystem.onActionChange += HandleActionChange;
        }

        private static void HandleActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed)
                return;
            if (obj is not InputAction action)
                return;

            var device = action.activeControl?.device;
            if (device == null)
                return;

            InputSchemeType scheme;
            if (device is Gamepad || device is Joystick)
                scheme = InputSchemeType.Gamepad;
            else if (device is Keyboard || device is Mouse)
                scheme = InputSchemeType.KeyboardMouse;
            else
                return; //其他裝置（如 Sensor）不影響提示 icon

            if (scheme == CurrentScheme)
                return;

            CurrentScheme = scheme;
            Debug.Log("[InputSchemeWatcher] Scheme changed to " + scheme + " change:" + change);
            OnSchemeChanged?.Invoke(scheme);
        }
    }
}
