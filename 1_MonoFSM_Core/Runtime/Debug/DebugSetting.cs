using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace MonoDebugSetting
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class DebugSetting
    {
        
        public static bool Is2DFXEnabledInEditor
        {
            get => DebugSettingDict[nameof(Is2DFXEnabledInEditor)];
            set => SetBoolProperty(nameof(Is2DFXEnabledInEditor), value);
        }
        
        public static bool IsRecordFSM
        {
            get => DebugSettingDict[nameof(IsRecordFSM)];
            set => SetBoolProperty(nameof(IsRecordFSM), value);
        }
        // public static RCGBuildConfig BuildConfig
        // {
        //     get
        //     {
        //         if (_buildConfig == null)
        //         {
        //             _buildConfig = Resources.Load<RCGBuildConfig>("Configs/BuildVer/0_BuildConfig_Editor_Dev");
        //         }
        //
        //         return _buildConfig;
        //     }
        //     set => _buildConfig = value;
        // }
        //
        // public static RCGBuildConfig _buildConfig; 

        public static bool IsPlayerDebugInvincible
        {
            get => DebugSettingDict[nameof(IsPlayerDebugInvincible)];
            set => SetBoolProperty(nameof(IsPlayerDebugInvincible), value);
        }

        private static readonly Dictionary<string, bool> DebugSettingDict = new();
        public static IEnumerable<string> DebugModuleNames => DebugSettingDict.Keys;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
#endif
        private static void Init()
        {
            // _isSpeedUpActionEnabled = BoolProperties[nameof(IsSpeedUpActionEnabled)];
            // if (IsDebugMode)
            //     GuidManager.InitRuntime();
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        //FIXME: debug ui?
        
        static DebugSetting()
        {
            foreach (var property in typeof(DebugSetting).GetProperties())
            {
                
                if (property.PropertyType != typeof(bool)) continue;
#if UNITY_EDITOR
                var value = EditorPrefs.GetBool(property.Name, false);
#else
                
                var value = false;
#endif
                if (PlayerPrefs.HasKey(property.Name))
                    value = PlayerPrefs.GetInt(property.Name, 0) == 1;
                DebugSettingDict[property.Name] = value;
                if(property.SetMethod != null)
                    property.SetValue(null, value);
            }

#if !UNITY_EDITOR
            IsProductionMode = true;
#endif
          
        }

        //之後應該看這個
        public static TestMode mode => IsProductionMode ? TestMode.Production : TestMode.EditorDevelopment;

        public static bool IsRuntimeQAEnabled
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsRuntimeQAEnabled)];
            set => SetBoolProperty(nameof(IsRuntimeQAEnabled), value);
#else
            get => false;
            set { }
#endif
        }

        public static bool IsLogSound
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsLogSound)];
            set => SetBoolProperty(nameof(IsLogSound), value);
#else
get => false;
            set { }
#endif
        }
        public static bool IsProductionMode //乾淨存檔，不會有提前拿到能力
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsProductionMode)];
            set => SetBoolProperty(nameof(IsProductionMode), value);
#else

            get => true;
            set{}
#endif            
        }

        public static bool IsShowingTileRenderer
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsShowingTileRenderer)];
            set => SetBoolProperty(nameof(IsShowingTileRenderer), value);
#else
             get => false;
             set {}
#endif
        }

        public static bool IsShowingSkin
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsShowingSkin)];
            set => SetBoolProperty(nameof(IsShowingSkin), value);
#else
             get => true;
             set {}
#endif
        }
        
        public static bool IsShowDebugNumber
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsShowDebugNumber)] || IsDebugMode;
            set => SetBoolProperty(nameof(IsShowDebugNumber), value);
#else
             get => false;
             set {}
#endif
        }
        // public static DebugCheatNode debugNode;
        
        // 所有的測試view / 快捷鍵都要綁這個

        private static bool _isIgnoreIgnoringCullingActivated;

        // private static bool _isSpeedUpActionEnabled;

        // public static bool IsSpeedUpActionEnabled
        // {
        //     get => _isSpeedUpActionEnabled;
        //     set
        //     {
        //         _isSpeedUpActionEnabled = value;
        //         SetBoolProperty(nameof(IsSpeedUpActionEnabled), value);
        //     }
        // }

        public static bool IsIgnoreCullingActivated
        {
#if RCG_DEV
            get => _isIgnoreIgnoringCullingActivated;
            set
            {
                _isIgnoreIgnoringCullingActivated = value;
                SetBoolProperty(nameof(IsIgnoreCullingActivated), value);
            }
#else
             get => false;
             set {}
#endif
        }

        public static bool IsShowAllFields
        {
            get => DebugSettingDict[nameof(IsShowAllFields)];
            set => SetBoolProperty(nameof(IsShowAllFields), value);
        }
        public static bool IsDrawCustomGizmo = true;

        public static bool DebugModuleEnabled(System.Type type)
        {
            return DebugSettingDict.ContainsKey(type.Name);
        }

        public static bool DebugModuleEnabled(string moduleName)
        {
            return DebugSettingDict.ContainsKey(moduleName) && DebugSettingDict[moduleName];
        }

        // [Command("module.toggleDebug")]
        // public static void ToggleDebugModule([DebugModule] string moduleName)
        // {
        //     DebugSettingDict.TryAdd(moduleName, false);
        //     SetBoolProperty(moduleName, !DebugSettingDict[moduleName]);
        //     QuantumConsole.Instance.LogToConsole($"{moduleName} is " +
        //                                          (DebugSettingDict[moduleName] ? "enabled" : "disabled"));
        // }
        //
        // [Command("module.isEnabled")]
        // private static void IsModuleEnabled([DebugModule] string moduleName)
        // {
        //     var result = IsBoolPropertyEnabled(moduleName);
        //     QuantumConsole.Instance.LogToConsole($"{moduleName} is " + (result ? "enabled" : "disabled"));
        // }
        
        public static bool IsDebugMode
        {
            //FIXME: 怎麼從這邊拿...
#if UNITY_EDITOR
            get => MonoFSMDebugSetting.IsDebugMode;
#else
            get => false;
#endif
//             //為什麼之前要註解掉editor if?
//             
// #if RCG_DEV
//             // get => false;
//             get => DebugSettingDict[nameof(IsDebugMode)]; //這很慢...?
//             set
//             {
//                 SetBoolProperty(nameof(IsDebugMode), value);
//                 //進入debug mode就先無敵ㄅ
//                 // if (value) IsPlayerInvincible = true;
//             }
// #else
//              get => false;
//              set {}
// #endif
        }

        public static bool IsDisplayingAllSolvable
        {
#if RCG_DEV
            get => IsDebugMode || DebugSettingDict[nameof(IsDisplayingAllSolvable)];
            set => SetBoolProperty(nameof(IsDisplayingAllSolvable), value);
#else
             get => false;
             set {}
#endif
        }

        public static bool IsSceneTestMode
        {
            //為什麼之前要註解掉editor if?
#if UNITY_EDITOR
            get => DebugSettingDict[nameof(IsSceneTestMode)];
            set
            {
                SetBoolProperty(nameof(IsSceneTestMode), value);
                //進入debug mode就先無敵ㄅ
                // if (value) IsPlayerInvincible = true;
            }
#else
             get => false;
             set {}
#endif
        }

        public static bool PlayerOneHitKill
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(PlayerOneHitKill)];
            set => SetBoolProperty(nameof(PlayerOneHitKill), value);
#else
             get=>false;
             set{}
#endif
        }

        public static bool IsPlayerInfiniteMana
        {
#if RCG_DEV
            get => DebugSettingDict[nameof(IsPlayerInfiniteMana)];
            set => SetBoolProperty(nameof(IsPlayerInfiniteMana), value);
#else
             get=>false;
             set{}
#endif
        }

        public static bool SkipHackMiniGame
        {
            get => DebugSettingDict[nameof(SkipHackMiniGame)];
            set => SetBoolProperty(nameof(SkipHackMiniGame), value);
        }

        // [Command("test.PlayerOneHitKill")]
        private static void SetPlayerOneHitKill(bool activate)
        {
            PlayerOneHitKill = activate;
        }

        // [Command("test.PlayerInvincible")]
        private static void SetPlayerInvincible(bool activate)
        {
            IsPlayerDebugInvincible = activate;
        }

        // Save all properties to EditorPrefs when any one of them is set
        private static void SetPropertyValue(string propertyName, bool value)
        {
            DebugSettingDict[propertyName] = value;
            // Debug.Log($"DebugSetting Set {propertyName} to {value}");
#if UNITY_EDITOR
            PlayerPrefs.SetInt(propertyName, value ? 1 : 0);
            EditorPrefs.SetBool(propertyName, value);
#endif
        }

        // Use the dictionary to set the property and save to EditorPrefs
        public static void SetBoolProperty(string propertyName, bool value)
        {
            SetPropertyValue(propertyName, value);
        }

        private static bool IsBoolPropertyEnabled(string propertyName)
        {
            if (DebugSettingDict.ContainsKey(propertyName))
                return DebugSettingDict[propertyName];
            return false;
        }
    }
}