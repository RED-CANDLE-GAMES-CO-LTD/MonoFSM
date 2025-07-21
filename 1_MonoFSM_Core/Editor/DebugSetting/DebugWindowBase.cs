using System;
using System.Linq;
using System.Reflection;
using MonoDebugSetting;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace RCG.Core
{
    //Deprecated    
    [Obsolete]
    public class DebugWindowBase : OdinEditorWindow, IToolbarWindow
    {
        // private const string MenuName = "RCGs/Toggle DebugMode (Hierarchy Coloring)"; // #%_D
        //
        // [MenuItem(MenuName)]
        // private static void ToggleDebugMode()
        // {
        //     // RCGDebugSetting.IsDebugMode.SetValue(!RCGDebugSetting.IsDebugMode.value);
        //     
        //     // DebugSetting.IsDebugMode = !DebugSetting.IsDebugMode;
        //     // Debug.Log("DebugMode:" + RCGDebugSetting.IsDebugMode.value);
        //     OpenWindowStatic();
        //     //find types that inherit from DebugWindowBase
        //
        //
        //     // debugWindow.debugModeFlag.CurrentValue = IsDebugMode;
        // }

        public static DebugWindowBase OpenWindowStatic()
        {
            try
            {
                var types = TypeCache.GetTypesDerivedFrom(typeof(DebugWindowBase));
                foreach (var type in types)
                {
                    // Debug.Log(type);
                    // var method = type.GetMethod(nameof(OpenWindow));
                    return GetWindow(type) as DebugWindowBase;
                    // var window = method?.Invoke(null, null);
                    // Debug.Log(window);
                    // return window as DebugWindowBase;
                }
            }

            catch (ReflectionTypeLoadException e)
            {
                return GetWindow(typeof(DebugWindowBase)) as DebugWindowBase;
            }

            

            return null;
        }

        // [MenuItem(MenuName, true)]
        // private static bool ToggleActionValidate()
        // {
        //     Menu.SetChecked(MenuName, DebugSetting.IsDebugMode);
        //     return true;
        // }


        public virtual DebugWindowBase OpenWindow()
        {
            var window = GetWindow<DebugWindowBase>();
            return window;
            // window.IsDebugMode = !window.IsDebugMode;
            
        }

        // [ShowInInspector]
        // public bool IsDebugMode
        // {
        //     get => DebugSetting.IsDebugMode;
        //     set => DebugSetting.IsDebugMode = value;
        // }

        protected override void OnImGUI()
        {
            base.OnImGUI();
            //display all debug settings properties
            if (!DebugSetting.IsShowAllFields)
                return;
            var fields = typeof(DebugSetting).GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.PropertyType != typeof(bool)) continue;

                var value = field.GetValue(null);
                // Debug.Log(field.Name + ":" + value);
                var newValue = EditorGUILayout.Toggle(field.Name, (bool)value);
                if ((bool)value != newValue)
                {
                    field.SetValue(null, newValue);
                }
            }
        }

        // private void OnInspectorUpdate()
        // {
        //     
        // }
    }
}