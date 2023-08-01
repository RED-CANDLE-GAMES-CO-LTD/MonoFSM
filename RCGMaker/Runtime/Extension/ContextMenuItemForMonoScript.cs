using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RCGMaker.Core
{
    public static class ContextMenuItemForMonoScript
    {
#if UNITY_EDITOR

       
        
        [MenuItem("CONTEXT/MonoBehaviour/Filter Logs for me")]
        public static void FindLog(MenuCommand command)
        {
            var owner = command.context;
            var id = owner.GetInstanceID();
            EditorGUIUtility.systemCopyBuffer = id.ToString();

            var assembly = Assembly.GetAssembly(typeof(SceneView));
            var consoleWindowType = assembly.GetType("UnityEditor.ConsoleWindow");
            var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
            var setFilterMethod =
                consoleWindowType.GetMethod("SetFilter", BindingFlags.Instance | BindingFlags.NonPublic);
            setFilterMethod.Invoke(consoleWindow, new object[] { id.ToString() });
        }

        [MenuItem("CONTEXT/Component/HideFlag/None")]
        public static void ResetHideFlag(MenuCommand command)
        {
            var owner = command.context as Component;
            if (owner != null) owner.hideFlags = HideFlags.None;
            else
                Debug.LogError("Can't find Component");
        }

        [MenuItem("CONTEXT/Component/HideFlag/Lock")]
        public static void LockTransformHideFlag(MenuCommand command)
        {
            var owner = command.context as Component;
            if (owner != null) owner.hideFlags = HideFlags.NotEditable;
            else
                Debug.LogError("Can't find Component");
        }
#endif
    }
}