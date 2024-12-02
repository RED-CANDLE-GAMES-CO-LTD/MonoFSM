using RCGMaker.Core;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace RCGSetting
{

        public class DebugSetting<T> : UserSetting<T>
        {
            public DebugSetting(string key, T value)
                : base(RCGDebugSetting.settings, key, value, SettingsScope.User) { }
        }

        public static class RCGDebugSetting
        {
            public static Settings settings = new Settings("com.rcg.debug", "RCGDebugSetting");
            [UserSetting("User-specific preferences", 
                "Enable Debug Mode (Ctrl/Cmd + Shift + D)", 
                "打開debug mode")]
            public static DebugSetting<bool> IsDebugMode = new("rcg.isDebugMode", false);

            [InitializeOnLoadMethod]
            static void Init()
            {
                //rcgdev
                //這樣會很貴嗎XDD
                ScriptingDefineUtility.Add("RCG_DEV", EditorUserBuildSettings.selectedBuildTargetGroup, true);
            }
            // Shared team settings
            // [UserSetting("Auto-add BlackBox component", "To new Prefabs", 
            //     "Automatically adds a BlackBox component to all newly created Prefabs.")]
            // public static DebugSetting<bool> AutoAddToPrefabs = new("general.autoAddToPrefab", false);
        }

        static class RCGDebugSettingsProvider
        {
            const string SettingsPath = "RCG/Debug Setting";
            
            private const string MenuName = "RCG/Toggle DebugMode (Hierarchy Coloring) #%_D";

            [MenuItem(MenuName)]
            private static void ToggleDebugMode()
            {
                
                RCGDebugSetting.IsDebugMode.SetValue(!RCGDebugSetting.IsDebugMode.value);
                Debug.Log("ToggleDebugMode: "+RCGDebugSetting.IsDebugMode.value);
                EditorApplication.RepaintHierarchyWindow();
            }

            [SettingsProvider]
            static SettingsProvider CreateSettingsProvider()
            {
                UserSettingsProvider provider = new(SettingsPath,
                    RCGDebugSetting.settings,
                    new[] { typeof(RCGDebugSettingsProvider).Assembly }, SettingsScope.User)
                {
                    keywords = new[] { "Debug", "FSM" }
                };

                RCGDebugSetting.settings.afterSettingsSaved += OnSettingsSaved;
                return provider;
            }

            private static void OnSettingsSaved()
            {
                SaveSettingsRuntimeSide();
                // SceneWatcher.UpdateAllPrefabsInScene();
            }

            [InitializeOnLoadMethod]
            private static void SaveSettingsRuntimeSide()
            {
                // BlackBox.LockingDisabled = BlackBoxSettings.DisableLocking.value;
                // BlackBox.ApplyDisabledByDefault = BlackBoxSettings.ApplyDisabledByDefault.value;
            }
        }
}