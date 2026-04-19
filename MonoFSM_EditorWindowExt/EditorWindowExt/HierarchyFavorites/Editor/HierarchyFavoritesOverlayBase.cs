using UnityEditor;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    public abstract class HierarchyFavoritesOverlayBase : EditorWindow
    {
        private static HierarchyFavoritesOverlayBase _current;
        public static HierarchyFavoritesOverlayBase Current => _current;

        internal static void ShowOverForMode(HierarchyFavoritesSettings.OverlayMode mode, Rect hierarchyScreenRect)
        {
            var targetType = mode == HierarchyFavoritesSettings.OverlayMode.IMGUI
                ? typeof(HierarchyFavoritesOverlayIMGUI)
                : typeof(HierarchyFavoritesOverlayUIToolkit);

            //Debug.Log($"[HF-Base] ShowOverForMode mode={mode} rect={hierarchyScreenRect} hasCurrent={_current != null}");

            // 若已存在但型別不一致（切換了 mode），先關掉再建
            if (_current != null && _current.GetType() != targetType)
            {
                CloseCurrent();
            }

            var rect = ComputeOverlayRect(hierarchyScreenRect);

            if (_current != null)
            {
                _current.position = rect;
                _current.OnRebuild();
                _current.Repaint();
                //Debug.Log($"[HF-Base] reuse existing window, rect={rect}");
                return;
            }

            var inst = (HierarchyFavoritesOverlayBase)CreateInstance(targetType);
            inst.hideFlags = HideFlags.HideAndDontSave;
            _current = inst;
            inst.ShowPopup();
            inst.position = rect;
            inst.OnRebuild();
            inst.Repaint();
            //Debug.Log($"[HF-Base] created {targetType.Name}, rect={rect}");
        }

        public static void CloseCurrent()
        {
            if (_current == null) return;
            var c = _current;
            _current = null;
            //Debug.Log($"[HF-Base] CloseCurrent type={c.GetType().Name}");
            c.Close();
        }

        /// <summary>
        /// 清除 domain reload 後殘留的 overlay popup 視窗。
        /// compile 中斷時 _current static 被重置但 Unity 的 window 還存在。
        /// </summary>
        public static void CleanupOrphanWindows()
        {
            var all = Resources.FindObjectsOfTypeAll<HierarchyFavoritesOverlayBase>();
            foreach (var w in all)
            {
                if (w == null) continue;
                try { w.Close(); }
                catch { /* 忽略關閉時的 exception */ }
                if (w != null) Object.DestroyImmediate(w);
            }
            _current = null;
        }

        [MenuItem("Tools/Hierarchy Favorites/Cleanup Orphan Overlays")]
        private static void CleanupOrphanWindowsMenu()
        {
            CleanupOrphanWindows();
            Debug.Log("[HierarchyFavorites] Cleaned up orphan overlay windows.");
        }

        private static Rect ComputeOverlayRect(Rect hierarchyRect)
        {
            const float margin = 4f;
            float height = Mathf.Max(160f, hierarchyRect.height * 0.6f);
            return new Rect(
                hierarchyRect.x + margin,
                hierarchyRect.y + margin,
                hierarchyRect.width - margin * 2f,
                height);
        }

        protected virtual void OnDisable()
        {
            if (_current == this) _current = null;
        }

        /// <summary>請求重新構建 UI / 重繪</summary>
        protected abstract void OnRebuild();

        protected static void HandleEntryClick(Transform target)
        {
            //Debug.Log($"[HierarchyFavorites] Clicked entry for {target}", target);
            if (target == null)
            {
                //Debug.LogError("[HierarchyFavorites] Clicked entry with null target!");
                return;
            }

            Selection.activeTransform = target;
            EditorGUIUtility.PingObject(target);
        }
    }
}