using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    [InitializeOnLoad]
    internal static class HierarchyFavoritesTrigger
    {
        private const string HierarchyWindowTypeName = "SceneHierarchyWindow";

        private static bool _altHeld;
        private static EditorWindow _lastHierarchy;

        static HierarchyFavoritesTrigger()
        {
            // 清除上次 domain reload 殘留的 overlay 視窗（compile 中斷時會留下）
            HierarchyFavoritesOverlayBase.CleanupOrphanWindows();

            EditorApplication.update += Tick;
            EditorApplication.modifierKeysChanged += OnModifierKeysChanged;
            // AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
        }

        // private static void OnBeforeReload()
        // {
        //     // reload 前主動關閉，避免殘留
        //     _altHeld = false;
        //     HierarchyFavoritesOverlay.CloseCurrent();
        //     HierarchyFavoritesOverlayBase.CleanupOrphanWindows();
        // }

        // === Alt 實際鍵盤狀態偵測（不依賴 GUI event） ===
#if UNITY_EDITOR_OSX
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern ulong CGEventSourceFlagsState(int stateID);

        private const int kCGEventSourceStateCombinedSessionState = 0;
        private const ulong kCGEventFlagMaskAlternate = 0x00080000;

        private static bool IsAltPressedNow()
        {
            try
            {
                return (CGEventSourceFlagsState(kCGEventSourceStateCombinedSessionState) & kCGEventFlagMaskAlternate) !=
                       0;
            }
            catch
            {
                return false;
            }
        }
#elif UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_MENU = 0x12;

        private static bool IsAltPressedNow()
        {
            try { return (GetAsyncKeyState(VK_MENU) & 0x8000) != 0; }
            catch { return false; }
        }
#else
        private static bool IsAltPressedNow() => false;
#endif

        private static bool IsHierarchy(EditorWindow w) =>
            w != null && w.GetType().Name == HierarchyWindowTypeName;

        private static bool IsOverlay(EditorWindow w) =>
            w is HierarchyFavoritesOverlayBase;

        private static EditorWindow ResolveTarget()
        {
            var mouseOver = EditorWindow.mouseOverWindow;
            if (IsHierarchy(mouseOver) || IsOverlay(mouseOver)) return mouseOver;
            // var focused = EditorWindow.focusedWindow;
            // if (IsHierarchy(focused) || IsOverlay(focused)) return focused;
            return null;
        }

        // 主要入口：modifier 狀態改變時，直接用原生 API 查 Alt 實際狀態並開關 overlay。
        private static void OnModifierKeysChanged()
        {
            EvaluateAndToggle();
        }

        private static void EvaluateAndToggle()
        {
            bool altNow = IsAltPressedNow();
            var target = ResolveTarget();
            if (IsHierarchy(target)) _lastHierarchy = target;

            bool overValidTarget = target != null;

            if (altNow && overValidTarget && _lastHierarchy != null)
            {
                if (!_altHeld)
                {
                    _altHeld = true;
                    Debug.Log($"[HF-Trigger] ShowOver (alt={altNow}, target={target?.GetType().Name})");
                    HierarchyFavoritesOverlay.ShowOver(_lastHierarchy.position);
                }
            }
            else if (_altHeld && (!altNow || !overValidTarget))
            {
                _altHeld = false;
                Debug.Log($"[HF-Trigger] CloseCurrent (alt={altNow}, target={target?.GetType().Name})");
                HierarchyFavoritesOverlay.CloseCurrent();
            }
        }

        // Tick: modifierKeysChanged 不一定每次都 fire（mouse 在 overlay 內放開 Alt 等邊界情況），
        // 所以用 10Hz poll 兜底，成本極低。
        private static void Tick()
        {
            EvaluateAndToggle();
        }
    }
}