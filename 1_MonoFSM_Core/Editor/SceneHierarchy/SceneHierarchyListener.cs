using Fusion.Addons.FSM;
using MonoFSM.Editor;
using MonoFSM.InternalBridge;
using RCGMaker.Core;
using UnityEditor;

namespace _1_MonoFSM_Core.Editor.SceneHierarchy
{
    public static class SceneHierarchyListener
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Register the SceneHierarchyUtility to listen for changes in the scene hierarchy
            // SceneHierarchyUtility.OnSceneHierarchyChanged += OnSceneHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode)
                // WorldReseter.CurrentWorldManager
                EditorFsmEventManager.OnStateChanged += OnFsmEventManagerOnOnStateChanged;
            else if (obj == PlayModeStateChange.ExitingPlayMode)
                EditorFsmEventManager.OnStateChanged -= OnFsmEventManagerOnOnStateChanged;
        }

        private static void OnFsmEventManagerOnOnStateChanged(StateMachineLogic stateMachine)
        {
            // Debug.Log($"State changed from {from} to {to} in {stateMachine}");
            SceneHierarchyUtility.TryRepaintHierarchy();
        }
    }
}