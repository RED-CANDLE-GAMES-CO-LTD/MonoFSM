using UnityEditor;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    /// <summary>
    /// Static facade：根據 HierarchyFavoritesSettings.Mode 選擇要顯示哪個 overlay 實作。
    /// </summary>
    public static class HierarchyFavoritesOverlay
    {
        public static bool IsOpen => HierarchyFavoritesOverlayBase.Current != null;

        public static HierarchyFavoritesOverlayBase CurrentWindow => HierarchyFavoritesOverlayBase.Current;

        public static void ShowOver(Rect hierarchyScreenRect)
        {
            var mode = HierarchyFavoritesSettings.Mode;
            HierarchyFavoritesOverlayBase.ShowOverForMode(mode, hierarchyScreenRect);
        }

        public static void CloseCurrent()
        {
            HierarchyFavoritesOverlayBase.CloseCurrent();
        }
    }
}
