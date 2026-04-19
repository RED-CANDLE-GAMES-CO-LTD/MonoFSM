using UnityEditor;

namespace HierarchyFavorites.Editor
{
    internal static class HierarchyFavoritesSettings
    {
        public enum OverlayMode
        {
            UIToolkit = 0,
            IMGUI = 1,
        }

        private const string PrefKey = "HierarchyFavorites.OverlayMode";

        public static OverlayMode Mode
        {
            get => (OverlayMode)EditorPrefs.GetInt(PrefKey, (int)OverlayMode.IMGUI);
            set => EditorPrefs.SetInt(PrefKey, (int)value);
        }

        private const string MenuRoot = "Tools/Hierarchy Favorites/";

        [MenuItem(MenuRoot + "Mode: UI Toolkit")]
        private static void SetUIToolkit() => Mode = OverlayMode.UIToolkit;
        [MenuItem(MenuRoot + "Mode: UI Toolkit", true)]
        private static bool SetUIToolkitCheck() { Menu.SetChecked(MenuRoot + "Mode: UI Toolkit", Mode == OverlayMode.UIToolkit); return true; }

        [MenuItem(MenuRoot + "Mode: IMGUI")]
        private static void SetIMGUI() => Mode = OverlayMode.IMGUI;
        [MenuItem(MenuRoot + "Mode: IMGUI", true)]
        private static bool SetIMGUICheck() { Menu.SetChecked(MenuRoot + "Mode: IMGUI", Mode == OverlayMode.IMGUI); return true; }
    }
}
