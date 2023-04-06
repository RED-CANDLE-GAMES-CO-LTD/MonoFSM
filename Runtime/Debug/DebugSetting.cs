using System.Collections.Generic;
// using QFSW.QC;
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
        public static bool IsShowDebugNumber
        {
            get => BoolProperties[nameof(IsShowDebugNumber)];
            set => SetBoolProperty(nameof(IsShowDebugNumber), value);
        }


        // public static DebugCheatNode debugNode;
        public static bool IsDebugMode
        {
#if UNITY_EDITOR
            get => BoolProperties[nameof(IsDebugMode)];
            set => SetBoolProperty(nameof(IsDebugMode), value);
#else
            get => false;
            set {}
#endif
        }

        public static bool PlayerOneHitKill
        {
#if UNITY_EDITOR
            get => BoolProperties[nameof(PlayerOneHitKill)];
            set => SetBoolProperty(nameof(PlayerOneHitKill), value);
#else
            get=>false;
            set{};
#endif
        }

        public static bool IsPlayerInvincible;

        // [Command("test.PlayerOneHitKill")]
        private static void SetPlayerOneHitKill(bool activate)
        {
            PlayerOneHitKill = activate;
        }

        // [Command("test.PlayerInvincible")]
        private static void SetPlayerInvincible(bool activate)
        {
            IsPlayerInvincible = activate;
        }

//         static DebugSetting()
//         {
// #if UNITY_EDITOR
//             _isDebugMode = EditorPrefs.GetBool("HierarchyDebugMode", false);
//             IsPlayerInvincible = EditorPrefs.GetBool("IsPlayerInvincible", true);
//             _PlayerOneHitKill = EditorPrefs.GetBool("PlayerOneHitKill", true);
// #else
//              _isDebugMode = false;
// #endif
//             
//         }
        private static readonly Dictionary<string, bool> BoolProperties = new();

        static DebugSetting()
        {
            foreach (var property in typeof(DebugSetting).GetProperties())
            {
                if (property.PropertyType != typeof(bool)) continue;
                var value = EditorPrefs.GetBool(property.Name, false);
                BoolProperties[property.Name] = value;
                property.SetValue(null, value);
            }
        }

        // Save all properties to EditorPrefs when any one of them is set
        private static void SetPropertyValue(string propertyName, bool value)
        {
            BoolProperties[propertyName] = value;
            EditorPrefs.SetBool(propertyName, value);
        }

        // Use the dictionary to set the property and save to EditorPrefs
        private static void SetBoolProperty(string propertyName, bool value)
        {
            if (!BoolProperties.ContainsKey(propertyName))
            {
                Debug.LogError($"Property {propertyName} does not exist.");
                return;
            }

            SetPropertyValue(propertyName, value);
        }
    }
}