using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    /// <summary>
    /// 一般的 dockable 視窗版本（非 Alt popup），內容與 overlay 相同（Favorites/Variables tab + 搜尋）。
    /// 快捷鍵 Alt+1 開啟/聚焦。
    /// </summary>
    public class HierarchyFavoritesWindow : EditorWindow
    {
        //&1 = Alt+1
        [MenuItem("Tools/Hierarchy Favorites/Open Window &1")]
        private static void Open()
        {
            var window = GetWindow<HierarchyFavoritesWindow>("Favorites");
            window.Rebuild();
            Debug.Log("[HierarchyFavorites] Open dockable window (Alt+1)", window);
        }

        [MenuItem("Tools/Hierarchy Favorites/Open Window &Tab")]
        private static void Tab()
        {
            Debug.Log("[HierarchyFavorites] Tab");
        }

        private void OnEnable()
        {
            // 收集邏輯依賴目前 selection / prefab stage，狀態變化時都要重建
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += Rebuild;
            PrefabStage.prefabStageOpened += OnPrefabStageChanged;
            PrefabStage.prefabStageClosing += OnPrefabStageChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.hierarchyChanged -= Rebuild;
            PrefabStage.prefabStageOpened -= OnPrefabStageChanged;
            PrefabStage.prefabStageClosing -= OnPrefabStageChanged;
        }

        private void OnSelectionChanged()
        {
            // 自己 HandleEntryClick 造成的 selection 變化不需要重建（內容沒變，重建只會弄掉 scroll/focus）
            if (HierarchyFavoritesOverlayBase.IsSelfSelectionChange)
            {
                //Debug.Log("[HierarchyFavorites] Skip rebuild for self selection change");
                return;
            }

            Rebuild();
        }

        private void OnPrefabStageChanged(PrefabStage stage)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            HierarchyFavoritesContentBuilder.Build(rootVisualElement, Rebuild, " (Window)");
        }
    }
}
