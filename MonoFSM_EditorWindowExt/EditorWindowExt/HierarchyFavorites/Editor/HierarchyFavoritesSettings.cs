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

        public enum ContentMode
        {
            //既有值的數字不可改（EditorPrefs 已存），新值往後加
            Favorites = 0,
            Variables = 1,
            Effects = 2,
            States = 3,
            Descriptions = 4,
        }

        private const string PrefKey = "HierarchyFavorites.OverlayMode";
        private const string ContentPrefKey = "HierarchyFavorites.ContentMode";

        public static OverlayMode Mode
        {
            get => (OverlayMode)EditorPrefs.GetInt(PrefKey, (int)OverlayMode.IMGUI);
            set => EditorPrefs.SetInt(PrefKey, (int)value);
        }

        public static ContentMode Content
        {
            get => (ContentMode)EditorPrefs.GetInt(ContentPrefKey, (int)ContentMode.Descriptions);
            set => EditorPrefs.SetInt(ContentPrefKey, (int)value);
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
