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

    //給「顯示提示 icon」用的機種軸，跟 InputSchemeType（gameplay 用，只分鍵鼠/手把）正交。
    //不要塞進 InputSchemeType：那個 enum 服務 gameplay 邏輯，加值容易讓既有的 switch(_ => false) 靜默失效。
    public enum PromptDeviceFamily
    {
        KeyboardMouse,
        GamepadGeneric, //認不出機種時的 fallback
        Xbox,
        PlayStation,
        Switch,
    }

    //取代已移除的 PlayerInputBinder：用最後觸發 action 的裝置判斷目前輸入方式
    public static class InputSchemeWatcher
    {
        public static InputSchemeType CurrentScheme { get; private set; } =
            InputSchemeType.KeyboardMouse;

        public static event Action<InputSchemeType> OnSchemeChanged;

        private static PromptDeviceFamily _currentDeviceFamily = PromptDeviceFamily.KeyboardMouse;

#if UNITY_EDITOR
        //Editor preview 用：非 Play Mode 時沒有任何輸入事件會發生，直接讓 Inspector 指定要預覽哪個機種的 icon
        public static PromptDeviceFamily EditorPreviewFamily = PromptDeviceFamily.KeyboardMouse;
#endif

        public static PromptDeviceFamily CurrentDeviceFamily
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return EditorPreviewFamily;
#endif
                return _currentDeviceFamily;
            }
        }

        public static event Action<PromptDeviceFamily> OnDeviceFamilyChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            //domain reload 關閉時避免重複註冊
            InputSystem.onActionChange -= HandleActionChange;
            InputSystem.onActionChange += HandleActionChange;

            InputSystem.onDeviceChange -= HandleDeviceChange;
            InputSystem.onDeviceChange += HandleDeviceChange;

            //刻意不在啟動時掃已連接裝置：手把插著但玩家用鍵鼠玩很常見（Steam 上尤其），
            //一開機就顯示手把圖是錯的。維持 KeyboardMouse 預設，等玩家真的動手把才切。
            _activeDevice = null;
        }

        //目前正在驅動 CurrentDeviceFamily 的裝置。只有它被拔掉才需要重新解析 family，
        //新裝置「插上」不主動切 —— 要等它真的產生輸入（HandleActionChange）。
        private static InputDevice _activeDevice;

        //手把插著沒人動也會不斷冒 ActionPerformed（搖桿零點漂移、扳機殘值），
        //光靠事件會一直把顯示從鍵鼠搶走。要求超過這個量值才算「玩家真的動手把」。
        private const float GamepadActuationThreshold = 0.5f;

        private static void HandleActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed)
                return;
            if (obj is not InputAction action)
                return;

            var control = action.activeControl;
            var device = control?.device;
            if (device == null)
                return;

            InputSchemeType scheme;
            if (device is Gamepad || device is Joystick)
            {
                //EvaluateMagnitude 對不支援量值的 control 會回 -1，那種情況（純 button）就直接放行
                var magnitude = control.EvaluateMagnitude();
                if (magnitude >= 0f && magnitude < GamepadActuationThreshold)
                    return;
                scheme = InputSchemeType.Gamepad;
            }
            else if (device is Keyboard || device is Mouse)
                scheme = InputSchemeType.KeyboardMouse;
            else
                return; //其他裝置（如 Sensor）不影響提示 icon

            if (scheme != CurrentScheme)
            {
                CurrentScheme = scheme;
                //FIXME: 好像怪怪的？
                // Debug.Log("[InputSchemeWatcher] Scheme changed to " + scheme + " change:" + change);
                OnSchemeChanged?.Invoke(scheme);
            }

            _activeDevice = device;
            SetDeviceFamily(GetFamilyOf(device));
        }

        //只處理「正在用的裝置被拔掉」：這時沒有輸入可以等，必須立刻退到一個合理的 family，
        //否則玩家會盯著一個已經不存在的手把圖示。
        //裝置「插上」刻意不處理 —— 插上不代表要用，等它產生輸入再切（見 HandleActionChange）。
        private static void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Removed && change != InputDeviceChange.Disconnected)
                return;
            if (device != _activeDevice)
                return;

            _activeDevice = null;
            RefreshDeviceFamilyFromConnectedDevices();
        }

        //正在用的裝置沒了才會走到這：優先挑還連著的手把（玩家本來就在用手把），沒有就回鍵鼠
        private static void RefreshDeviceFamilyFromConnectedDevices()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device == null || !device.enabled)
                    continue;
                if (device is Gamepad || device is Joystick)
                {
                    _activeDevice = device; //換這顆接手，之後它被拔掉也要能再退一次
                    SetDeviceFamily(GetFamilyOf(device));
                    return;
                }
            }

            SetDeviceFamily(PromptDeviceFamily.KeyboardMouse);
        }

        private static void SetDeviceFamily(PromptDeviceFamily family)
        {
            if (family == _currentDeviceFamily)
                return;
            _currentDeviceFamily = family;
            OnDeviceFamilyChanged?.Invoke(family);
        }

        private static PromptDeviceFamily GetFamilyOf(InputDevice device)
        {
            if (device is Keyboard || device is Mouse)
                return PromptDeviceFamily.KeyboardMouse;

            //判定順序很重要：機種要在 generic Gamepad 之前判斷，不然全部都會落到 GamepadGeneric
            if (Based(device, "DualShockGamepad"))
                return PromptDeviceFamily.PlayStation;
            if (Based(device, "SwitchProControllerHID") || Based(device, "SwitchProController"))
                return PromptDeviceFamily.Switch;
            if (Based(device, "XInputController"))
                return PromptDeviceFamily.Xbox;

            if (device is Gamepad || device is Joystick)
                return PromptDeviceFamily.GamepadGeneric;

            return _currentDeviceFamily; //其他裝置（如 Sensor）不影響 family
        }

        //用 layout 繼承關係判斷機種，不要比對產品名字串（本地化/OS 差異會讓字串比對失效）
        private static bool Based(InputDevice device, string layout)
        {
            return InputSystem.IsFirstLayoutBasedOnSecond(device.layout, layout);
        }
    }
}
