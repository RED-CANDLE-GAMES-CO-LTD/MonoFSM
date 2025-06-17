using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core
{
    public static class ContextMenuItemForMonoScript
    {
#if UNITY_EDITOR
        private const string RevertPrefabTransformMenuName =
            "CONTEXT/RectTransform/Remove Override For RectTransform in Children";

        [MenuItem(RevertPrefabTransformMenuName)]
        public static void RemoveOverrideForRectTransforms(MenuCommand command)
        {
            var rectTransform = command.context as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogError("Can't find RectTransform");
                return;
            }

            var rects = rectTransform.GetComponentsInChildren<RectTransform>(true);
            Debug.Log("Remove override for RectTransform in children: " + rects.Length, rectTransform);
            foreach (var rect in rects)
            {
                try
                {
                    RemoveRectTransformOverride(rect);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e + e.StackTrace, rect);
                }
                
            }
        }

        private static void RemoveRectTransformOverride(RectTransform rectTransform)
        {
            //check prefab override for RectTransform
            //FIXME:應該只拿一層ㄅ

            //check if it is a prefab instance
            if (!PrefabUtility.IsPartOfPrefabInstance(rectTransform))
            {
                // Debug.LogWarning("Not a prefab instance", rectTransform);
                return;
            }
            
            
            var serObj = new SerializedObject(rectTransform);
            var prop = serObj.GetIterator();
            while (prop.NextVisible(true))
            {
                PrefabUtility.RevertPropertyOverride(prop, InteractionMode.UserAction);
            }
        }

        [MenuItem(RevertPrefabTransformMenuName, validate = true)]
        private static bool RevertPrefabTransformValidate(MenuCommand command)
        {
            var obj = command.context;
            return PrefabUtility.IsPartOfPrefabInstance(obj);
        }
        
       
        
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