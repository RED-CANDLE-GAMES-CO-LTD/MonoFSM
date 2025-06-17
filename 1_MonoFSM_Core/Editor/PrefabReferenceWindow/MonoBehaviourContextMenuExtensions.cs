// #if UNITY_EDITOR

using MonoFSM.InternalBridge;
using UnityEditor;
using UnityEngine;

public static class MonoBehaviourContextMenuExtensions
{
    [MenuItem("CONTEXT/MonoBehaviour/打開 Prefab Variable Reference Finder")]
    private static void OpenPrefabReferenceFinder(MenuCommand command)
    {
        var mono = command.context as MonoBehaviour;
        if (mono == null) return;
        // 開啟視窗
        MonoVarInPrefabReferenceWindow.ShowWindow();
        // 嘗試自動選定該 MonoBehaviour 作為 _searchTarget
        // 透過 EditorApplication.delayCall 保證視窗已建立
        EditorApplication.delayCall += () =>
        {
            var hierarchyWindow = WindowDocker.GetSceneHierarchyWindow;
            if (hierarchyWindow == null)
            {
                Debug.LogError("SceneHierarchyWindow not found");
                return;
            }

            var window = EditorWindow.GetWindow<MonoVarInPrefabReferenceWindow>();
            // var consoleWindow = GetWindow(typeof(SceneView));
            hierarchyWindow.Dock(window, WindowDocker.DockPosition.Bottom);

            if (window != null)
            {
                // 透過反射設定 private 欄位 _searchTarget
                var field = typeof(MonoVarInPrefabReferenceWindow).GetField("_searchTarget",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null && field.FieldType.IsAssignableFrom(mono.GetType()))
                {
                    field.SetValue(window, mono);
                    // 觸發搜尋結果更新
                    var updateMethod = typeof(MonoVarInPrefabReferenceWindow).GetMethod("UpdateSearchResults",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    updateMethod?.Invoke(window, null);
                    window.Repaint();
                }
            }
        };
    }
}
// #endif