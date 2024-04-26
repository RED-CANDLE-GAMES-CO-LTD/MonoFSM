// using QFSW.QC;

using System.Collections.Generic;
using RCGInternal.BuildConfig;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RCGSetting
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

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
#endif
        private static void Init()
        {
            // _isSpeedUpActionEnabled = BoolProperties[nameof(IsSpeedUpActionEnabled)];
            if (IsDebugMode)
                GuidManager.InitRuntime();
        }
        
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
                DebugSettingDict[property.Name] = value;
                property.SetValue(null, value);
            }
        }

        //之後應該看這個
        public static TestMode mode => IsProductionMode ? TestMode.Build : TestMode.EditorDevelopment;

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

        public static bool IsProductionMode //乾淨存檔，不會有提前拿到能力
        {
#if UNITY_EDITOR
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
            get => DebugSettingDict[nameof(IsShowDebugNumber)];
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

        public static bool IsDrawCustomGizmo = true;
        public static bool IsDebugMode
        {
            //為什麼之前要註解掉editor if?
#if RCG_DEV
            // get => false;
            get => DebugSettingDict[nameof(IsDebugMode)]; //這很慢...?
            set
            {
                SetBoolProperty(nameof(IsDebugMode), value);
                //進入debug mode就先無敵ㄅ
                // if (value) IsPlayerInvincible = true;
            }
#else
             get => false;
             set {}
#endif
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

    


        public static void ToggleDebugMode()
        {
            IsDebugMode = !IsDebugMode;
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
            EditorPrefs.SetBool(propertyName, value);
#endif
        }

        // Use the dictionary to set the property and save to EditorPrefs
        private static void SetBoolProperty(string propertyName, bool value)
        {
            if (!DebugSettingDict.ContainsKey(propertyName))
            {
                Debug.LogError($"Property {propertyName} does not exist.");
                return;
            }

            SetPropertyValue(propertyName, value);
        }
    }
}