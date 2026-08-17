#if UNITY_EDITOR
using MonoFSM.EditorExtension;
using UnityEditor;
#endif
using MonoFSM_EditorWindowExt.EditorWindowExt;
using UnityEngine;

namespace MonoDebugSetting
{
//這段不能放到editor嗎？
#if UNITY_EDITOR
        // public class DebugSetting<T> : UserSetting<T>
        // {
        //     public DebugSetting(string key, T value)
        //         : base(RCGDebugSetting.settings, key, value, SettingsScope.User) { }
        // }

        public static class MonoFsmDebugSetting
        {
            // public static Settings settings = new Settings("com.rcg.debug", "RCGDebugSetting");
            // [UserSetting("User-specific preferences",
            //     "Enable Debug Mode (Ctrl/Cmd + Shift + D)",
            //     "打開debug mode")]
            // public static DebugSetting<bool> IsDebugMode = new("rcg.isDebugMode", false);
            public const string PrefPath = "MonoFSM.DebugSetting.IsDebugMode";
            private static bool _isDebugMode;

            public static bool IsDebugMode
            {
                get => _isDebugMode;
                set
                {
                    if (_isDebugMode == value) return;

                    _isDebugMode = value;
                    RuntimeDebugSetting.SetDebugMode(_isDebugMode);
                    HierarchyDebug.IsDebugMode = _isDebugMode;
                    EditorPrefs.SetBool(PrefPath, _isDebugMode);
                    EditorApplication.RepaintHierarchyWindow();
                    EditorWindowKeyboardNavigate.RepaintAll();
                    EditorWindowKeyboardNavigate.RepaintToolBar();

                    Debug.Log("ToggleDebugMode: " + _isDebugMode + " " +
                              EditorPrefs.GetBool(PrefPath, false));
                }
            }
            [InitializeOnLoadMethod]
            static void Init()
            {
                //FIXME: 要用什麼？
                //rcgdev
                //添加rcgdev的define
                // ScriptingDefineUtility.Add("RCG_DEV", EditorUserBuildSettings.selectedBuildTargetGroup, true);
                _isDebugMode = EditorPrefs.GetBool(PrefPath, false);
                RuntimeDebugSetting.SetDebugMode(_isDebugMode);
                HierarchyDebug.IsDebugMode = _isDebugMode;
            }

            // Shared team settings
            // [UserSetting("Auto-add BlackBox component", "To new Prefabs",
            //     "Automatically adds a BlackBox component to all newly created Prefabs.")]
            // public static DebugSetting<bool> AutoAddToPrefabs = new("general.autoAddToPrefab", false);

            private const string MenuName = "Tools/MonoFSM/Toggle DebugMode #%_D";

            [MenuItem(MenuName)]
            private static void ToggleDebugMode()
            {
                // RCGDebugSetting.IsDebugMode.SetValue(!RCGDebugSetting.IsDebugMode.value);
                IsDebugMode = !IsDebugMode;
            }

            private static void OnSettingsSaved()
            {
                SaveSettingsRuntimeSide();
            }

            [InitializeOnLoadMethod]
            private static void SaveSettingsRuntimeSide()
            {
                // BlackBox.LockingDisabled = BlackBoxSettings.DisableLocking.value;
                // BlackBox.ApplyDisabledByDefault = BlackBoxSettings.ApplyDisabledByDefault.value;
            }
        }
        #endif
}
