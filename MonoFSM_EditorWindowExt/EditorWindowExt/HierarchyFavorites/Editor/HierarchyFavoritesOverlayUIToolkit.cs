using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HierarchyFavorites.Editor
{
    public class HierarchyFavoritesOverlayUIToolkit : HierarchyFavoritesOverlayBase
    {
        private void OnEnable()
        {
            var root = rootVisualElement;
            root.pickingMode = PickingMode.Position;
            root.focusable = true;

            // trickle-down 攔截 drag 事件（在 button 等子元素之前）
            root.RegisterCallback<DragEnterEvent>(e => DragAndDrop.visualMode = DragAndDropVisualMode.Copy,
                TrickleDown.TrickleDown);
            root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            root.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
        }

        private void OnDragUpdated(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent e)
        {
            DragAndDrop.AcceptDrag();
            var refs = DragAndDrop.objectReferences;
            var added = HierarchyFavoritesDropHandler.HandleDrop(refs);
            Debug.Log($"[HierarchyFavorites] DragPerform refs={refs.Length} added={added}");
            if (added > 0) OnRebuild();
            e.StopPropagation();
        }

        protected override void OnRebuild()
        {
            Debug.Log("[HierarchyFavorites] Rebuild");
            // 內容構建共用邏輯抽到 HierarchyFavoritesContentBuilder，dockable window 也用同一份
            HierarchyFavoritesContentBuilder.Build(rootVisualElement, OnRebuild, " (UI Toolkit)");
        }
    }
}
