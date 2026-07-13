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
            // 收集邏輯依賴目前 selection / prefab stage，只在這兩者變化時重建。
            // 刻意不掛 EditorApplication.hierarchyChanged：它對 hierarchy 任何變動（改名、
            // 拖動、增減、Rename 按鈕…）都會觸發，會造成頻繁重建弄掉 scroll / focus。
            Selection.selectionChanged += OnSelectionChanged;
            PrefabStage.prefabStageOpened += OnPrefabStageChanged;
            PrefabStage.prefabStageClosing += OnPrefabStageChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
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
