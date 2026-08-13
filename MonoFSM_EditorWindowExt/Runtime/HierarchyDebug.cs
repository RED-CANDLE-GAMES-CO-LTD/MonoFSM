#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace MonoFSM.EditorExtension
{
    public static class HierarchyDebug
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            // Register the global event handler for escape key events
            EditorApplication.update += () =>
            {
                Profiler.BeginSample("HierarchyDebug Update");
                //FIXME: 再開一個setting?
                if (IsDebugMode && Application.isPlaying)
                    EditorApplication.RepaintHierarchyWindow();
                Profiler.EndSample();
            };
        }

        private static bool _isDebugMode = false;

        public static bool IsDebugMode
        {
            get => _isDebugMode;
            set
            {
                _isDebugMode = value;
                // if (_isDebugMode)
                //     Debug.Log("Hierarchy Debug Mode Enabled");
                // else
                //     Debug.Log("Hierarchy Debug Mode Disabled");
            }
        }
    }
}
#endif
