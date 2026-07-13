using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM_EditorWindowExt.EditorWindowExt
{
    public static class EditorWindowKeyboardNavigate
    {
        public static void ExpandItem(Object obj)
        {
            var window = SceneHierarchyWindow.lastInteractedHierarchyWindow;
            window.SetExpanded(obj.GetInstanceID(), true);
        }

        public static void RepaintToolBar()
        {
            Toolbar.get.Repaint();
        }

        public static void RepaintAll()
        {
            EditorApplication.RequestRepaintAllViews();
        }

        [MenuItem("Tools/MonoFSM/Clear Console Log &#_c ", false, 1000)]
        public static void ClearConsoleLog()
        {
            LogEntries.Clear();
            var h = SceneHierarchyWindow.lastInteractedHierarchyWindow;
            h.titleContent.text = "4 Hierarchy";
            if (ProjectBrowser.s_LastInteractedProjectBrowser != null)
                ProjectBrowser.s_LastInteractedProjectBrowser.titleContent.text = "5 Project";
            var p = ProjectBrowser.GetAllProjectBrowsers();
            // Debug.Log(
            //     "Cleared Console Log and updated Hierarchy and Project Browser titles."
            //         + $" Hierarchy: {h.titleContent.text}, Project Browsers: {p.Count}"
            // );
            foreach (var browser in p)
            {
                browser.titleContent.text = "5 Project";
            }
            // var windows = InspectorWindow.GetAllInspectorWindows();
        }

        private static double _lastEscapeTime;
        private static int _escapeCount;
        
        private static void ExitPrefabStage()
        {
            if (Application.isPlaying)
                return;

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.Log("Exiting Prefab Stage...");
                StageUtility.GoToMainStage();
            }
            // else
            // {
            //     Debug.Log("Not currently in Prefab Stage");
            // }
        }

        // [MenuItem("Tools/MonoFSM/Focus Hierarchy Window #%h")] // Cmd+Shift+H
        // private static void FocusHierarchyWindow()
        // {
        //     var hierarchyWindow = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor"));
        //     if (hierarchyWindow != null)
        //     {
        //         hierarchyWindow.Focus();
        //         Debug.Log("Focused on Hierarchy Window");
        //     }
        // }

        // [MenuItem("Tools/MonoFSM/Switch to Next Tab in Same Area #&_]")] // Shift+Tab key
        // private static void SwitchToNextTabInSameArea()
        // {
        //     SwitchTabInSameArea(1); // 1 = next
        //     RepaintToolBar();
        // }
        //
        // [MenuItem("Tools/MonoFSM/Switch to Previous Tab in Same Area #&_[")]
        // private static void SwitchToPreviousTabInSameArea()
        // {
        //     SwitchTabInSameArea(-1); // -1 = previous
        //     RepaintToolBar();
        // }

        private static void SwitchTabInSameArea(int direction)
        {
            try
            {
                var currentWindow = EditorWindow.focusedWindow;
                if (currentWindow == null)
                    return;

                // Get the dock area (container that holds multiple tabs)
                var dockArea = currentWindow.m_Parent as DockArea;
                if (dockArea == null)
                {
                    Debug.Log("No dock area found for the current window.");
                    return;
                }

                // Get all tabs in this dock area
                var panes = dockArea.m_Panes;
                if (panes == null || panes.Count <= 1)
                {
                    Debug.Log("No other tabs to switch to.");
                    return;
                }

                // Find current tab index
                int currentIndex = -1;
                for (int i = 0; i < panes.Count; i++)
                {
                    if (panes[i] == currentWindow)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                if (currentIndex == -1)
                    return;

                // Get target tab (循環)
                int targetIndex = (currentIndex + direction + panes.Count) % panes.Count;
                var targetWindow = panes[targetIndex];

                if (targetWindow != null)
                {
                    targetWindow.Focus();
                    string directionText = direction > 0 ? "next" : "previous";
                    Debug.Log(
                        $"Switched from {currentWindow.GetType().Name} to {targetWindow.GetType().Name} ({directionText} tab)"
                    );
                }
            }
            catch (Exception e)
            {
                string directionText = direction > 0 ? "next" : "previous";
                Debug.LogError($"Failed to switch to {directionText} tab: {e.Message}");
            }
        }

        // [MenuItem("Tools/MonoFSM/Switch to Right Dock Area #%_]")] // Tab key
        // private static void SwitchToRightDockArea()
        // {
        //     SwitchDockArea(1); // 1 = 往右
        //     RepaintToolBar();
        // }
        //
        // [MenuItem("Tools/MonoFSM/Switch to Left Dock Area #%_[")] // Shift+Tab key
        // private static void SwitchToLeftDockArea()
        // {
        //     SwitchDockArea(-1); // -1 = 往左
        //     RepaintToolBar();
        // }

        private static void SwitchDockArea(int direction)
        {
            try
            {
                var currentWindow = EditorWindow.focusedWindow;
                if (currentWindow == null)
                    return;

                // Get current dock area
                var currentDockArea = currentWindow.m_Parent as DockArea;
                if (currentDockArea == null)
                {
                    Debug.Log("No dock area found for the current window.");
                    return;
                }

                // Get the main window/container view
                var containerWindow = GetContainerWindow(currentDockArea);
                if (containerWindow == null)
                {
                    Debug.Log("No container window found for the current dock area.");
                    return;
                }

                // Get all dock areas in the container
                var allDockAreas = GetAllDockAreasInContainer(containerWindow);
                if (allDockAreas.Count <= 1)
                {
                    Debug.Log("No other dock areas to switch to.");
                    return;
                }

                // Find current dock area index
                int currentIndex = allDockAreas.IndexOf(currentDockArea);
                if (currentIndex == -1)
                {
                    Debug.Log("No current dock area found in the container.");
                    return;
                }

                // Get next dock area (循環)
                int nextIndex =
                    (currentIndex + direction + allDockAreas.Count) % allDockAreas.Count;
                var nextDockArea = allDockAreas[nextIndex];

                // Focus the first tab in the next dock area
                if (nextDockArea.m_Panes != null && nextDockArea.m_Panes.Count > 0)
                {
                    var targetWindow = nextDockArea.m_Panes[0];
                    targetWindow.Focus();
                    string directionText = direction > 0 ? "right" : "left";
                    Debug.Log(
                        $"Switched from {currentWindow.GetType().Name} to {targetWindow.GetType().Name} in {directionText} dock area"
                    );
                }
            }
            catch (Exception e)
            {
                string directionText = direction > 0 ? "right" : "left";
                Debug.LogError($"Failed to switch to {directionText} dock area: {e.Message}");
            }
        }

        private static void SwitchDockAreaVertical(int direction)
        {
            try
            {
                var currentWindow = EditorWindow.focusedWindow;
                if (currentWindow == null)
                    return;

                // Get current dock area
                var currentDockArea = currentWindow.m_Parent as DockArea;
                if (currentDockArea == null)
                {
                    Debug.Log("No dock area found for the current window.");
                    return;
                }

                // Get the main window/container view
                var containerWindow = GetContainerWindow(currentDockArea);
                if (containerWindow == null)
                {
                    Debug.Log("No container window found for the current dock area.");
                    return;
                }

                // Get all dock areas in the container sorted by vertical position
                var allDockAreas = GetAllDockAreasInContainer(containerWindow);
                if (allDockAreas.Count <= 1)
                {
                    Debug.Log("No other dock areas to switch to.");
                    return;
                }

                // Sort dock areas by their vertical position (Y coordinate)
                var sortedDockAreas = allDockAreas.OrderBy(da => da.screenPosition.y).ToList();

                // Find current dock area index in sorted list
                int currentIndex = sortedDockAreas.IndexOf(currentDockArea);
                if (currentIndex == -1)
                {
                    Debug.Log("No current dock area found in the container.");
                    return;
                }

                // Get next dock area vertically (循環)
                int nextIndex =
                    (currentIndex + direction + sortedDockAreas.Count) % sortedDockAreas.Count;
                var nextDockArea = sortedDockAreas[nextIndex];

                // Focus the first tab in the next dock area
                if (nextDockArea.m_Panes != null && nextDockArea.m_Panes.Count > 0)
                {
                    var targetWindow = nextDockArea.m_Panes[0];
                    targetWindow.Focus();
                    string directionText = direction > 0 ? "down" : "up";
                    Debug.Log(
                        $"Switched from {currentWindow.GetType().Name} to {targetWindow.GetType().Name} in {directionText} direction"
                    );
                }
            }
            catch (Exception e)
            {
                string directionText = direction > 0 ? "down" : "up";
                Debug.LogError($"Failed to switch to {directionText} dock area: {e.Message}");
            }
        }

        private static ContainerWindow GetContainerWindow(DockArea dockArea)
        {
            // Debug.Log($"Getting container window for dock area: {dockArea}");

            // Navigate up the hierarchy to find the main container
            var current = dockArea.parent;
            int level = 0;
            while (current != null)
            {
                // Debug.Log($"Level {level}: {current.GetType().Name}");

                // Check if we can find the container window through reflection
                var containerWindowField = current
                    .GetType()
                    .GetField("m_Window", BindingFlags.NonPublic | BindingFlags.Instance);
                if (containerWindowField != null)
                {
                    var containerWindow = containerWindowField.GetValue(current) as ContainerWindow;
                    if (containerWindow != null)
                    {
                        Debug.Log($"Found container window via m_Window field at level {level}");
                        return containerWindow;
                    }
                }

                // Also check if current itself might be a container-related type
                if (
                    current.GetType().Name.Contains("ContainerWindow")
                    || current.GetType().Name.Contains("MainView")
                )
                {
                    // Debug.Log($"Found container-related type: {current.GetType().Name}");
                    // Try to get container window from this view
                    var windowProperty = current
                        .GetType()
                        .GetProperty("window", BindingFlags.Public | BindingFlags.Instance);
                    if (
                        windowProperty != null
                        && windowProperty.GetValue(current) is ContainerWindow window
                    )
                    {
                        // Debug.Log($"Found container window via window property");
                        return window;
                    }
                }

                current = current.parent;
                level++;
            }
            Debug.Log("No container window found");
            return null;
        }

        private static List<DockArea> GetAllDockAreasInContainer(ContainerWindow containerWindow)
        {
            var dockAreas = new List<DockArea>();

            try
            {
                // Debug.Log($"Collecting dock areas from container window: {containerWindow}");
                // Get the root view from container window
                if (containerWindow.rootView != null)
                {
                    // Debug.Log($"Root view type: {containerWindow.rootView.GetType().Name}");
                    var visitedViews = new HashSet<View>();
                    CollectDockAreas(containerWindow.rootView, dockAreas, visitedViews);
                    // Debug.Log($"Total dock areas found: {dockAreas.Count}");
                }
                else
                {
                    Debug.Log("Container window has no root view");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error collecting dock areas: {e.Message}");
            }

            return dockAreas;
        }

        private static void CollectDockAreas(
            View view,
            List<DockArea> dockAreas,
            HashSet<View> visitedViews
        )
        {
            if (view == null)
                return;

            // Prevent infinite recursion
            if (visitedViews.Contains(view))
            {
                // Debug.Log($"Already visited view: {view.GetType().Name}, skipping");
                return;
            }
            visitedViews.Add(view);

            // Debug.Log($"Checking view: {view.GetType().Name}");

            // If this is a DockArea, add it
            if (view is DockArea dockArea)
            {
                // Debug.Log($"Found DockArea: {dockArea}");
                dockAreas.Add(dockArea);
                return;
            }

            // If this is a split view, recurse into children
            if (view is SplitView splitView)
            {
                // Debug.Log($"Found SplitView with {splitView.children.Length} children");
                foreach (var child in splitView.children)
                {
                    CollectDockAreas(child, dockAreas, visitedViews);
                }
            }
            // Handle MainView and other container views that might have children
            else if (
                view.GetType().Name == "MainView"
                || view.GetType().BaseType?.Name == "ContainerView"
            )
            {
                // Debug.Log($"Found MainView/ContainerView, using allChildren");

                if (view.allChildren != null && view.allChildren.Length > 0)
                {
                    // Debug.Log($"MainView has {view.allChildren.Length} children via allChildren");
                    foreach (var child in view.allChildren)
                    {
                        CollectDockAreas(child, dockAreas, visitedViews);
                    }
                }
                else
                {
                    // Debug.Log("MainView has no allChildren");
                }
            }
            else
            {
                // Debug.Log($"Unknown view type: {view.GetType().Name}");
            }
        }

        [Shortcut("Transform/MoveUp", KeyCode.LeftBracket, ShortcutModifiers.Alt)]
        private static void MoveUp()
        {
            Debug.Log("MoveUp triggered");
            var go = Selection.activeGameObject;
            if (go == null)
                return;
            var parent = go.transform.parent;
            if (parent == null)
                return;
            var index = go.transform.GetSiblingIndex();
            if (index > 0)
            {
                Undo.SetTransformParent(go.transform, parent, "Move Up");
                go.transform.SetSiblingIndex(index - 1);
                EditorUtility.SetDirty(go);
                // Debug.Log($"Moved {go.name} up to index {index - 1}");
            }
        }

        [Shortcut("Transform/MoveDown", KeyCode.RightBracket, ShortcutModifiers.Alt)]
        private static void MoveDown()
        {
            var go = Selection.activeGameObject;
            if (go == null)
                return;
            var parent = go.transform.parent;
            if (parent == null)
                return;
            var index = go.transform.GetSiblingIndex();
            if (index < parent.childCount - 1)
            {
                Undo.SetTransformParent(go.transform, parent, "Move Down");
                go.transform.SetSiblingIndex(index + 1);
                EditorUtility.SetDirty(go);
                // Debug.Log($"Moved {go.name} down to index {index + 1}");
            }
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.globalEventHandler -= HandleGlobalEscapeEvents; // Remove existing handler if any
            EditorApplication.globalEventHandler += HandleGlobalEscapeEvents; // Add our handler
        }

        // static bool _isTabPressed = false;
        // private static bool _isCustomNavigated = false;
        private static void HandleGlobalEscapeEvents()
        {
            if (Event.current == null)
                return;

            // Handle Cmd+Shift+Arrow keys combinations on KeyDown
            // if (Event.current.type == EventType.KeyDown)
            // {
            //     Debug.Log("GUIUtility.keyboardControl: " + GUIUtility.keyboardControl);
            //     //沒有按tab就不行耶
            //     // if (Event.current.keyCode == KeyCode.BackQuote)
            //     // {
            //     //     GUIUtility.keyboardControl = 0;
            //     //     _isTabPressed = true;
            //     //     Selection.activeObject = null;
            //     //     return;
            //     // }
            //
            //     // Check for Cmd+Shift+Arrow combinations
            //     if ((Event.current.keyCode == KeyCode.RightArrow || Event.current.keyCode == KeyCode.LeftArrow ||
            //          Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow)
            //         && Event.current.alt && Event.current.command)
            //     {
            //         Debug.Log($"Custom navigation triggered with key: {Event.current.keyCode}");
            //         switch (Event.current.keyCode)
            //         {
            //             case KeyCode.RightBracket:
            //                 SwitchDockArea(1); // Right
            //                 break;
            //             case KeyCode.LeftBracket:
            //                 SwitchDockArea(-1); // Left
            //                 break;
            //             case KeyCode.UpArrow:
            //                 SwitchDockAreaVertical(-1); // Up
            //                 break;
            //             case KeyCode.DownArrow:
            //                 SwitchDockAreaVertical(1); // Down
            //                 break;
            //
            //         }
            //
            //         _isCustomNavigated = true;
            //         Event.current.Use();
            //         return;
            //     }
            // }

            if (Event.current.type != EventType.KeyUp)
                return;

            // if (Event.current.keyCode == KeyCode.BackQuote)
            // {
            //     _isTabPressed = false;
            //     Debug.Log("isTabPressed set to false");
            //     if (_isCustomNavigated)
            //     {
            //         _isCustomNavigated = false;
            //         return;
            //     }
            //
            // }
            // Fallback: Handle Tab key alone for switching between dock areas (only if no custom switch was triggered)
            // if (Event.current.keyCode == KeyCode.Tab && !Event.current.shift && !Event.current.control && !Event.current.command && !Event.current.alt)
            // {
            //     // Debug.Log("Tab key pressed - Switching to right dock area...");
            //     SwitchToRightDockArea();
            //     Event.current.Use();
            //     return;
            // }
            //
            // // Handle Shift+Tab key for switching to left dock area
            // if (Event.current.keyCode == KeyCode.Tab && Event.current.shift && !Event.current.control && !Event.current.command && !Event.current.alt)
            // {
            //     // Debug.Log("Shift+Tab key pressed - Switching to left dock area...");
            //     SwitchToLeftDockArea();
            //     Event.current.Use();
            //     return;
            // }

            // if (Event.current.keyCode == KeyCode.Tab && Event.current.alt && !Event.current.control && !Event.current.command)
            // {
            //     // Debug.Log("Shift+Tab key pressed - Switching to left dock area...");
            //     SwitchToNextTabInSameArea();
            //     Event.current.Use();
            //     return;
            // }


            // Handle ESC key for exiting prefab stage
            if (Event.current.keyCode != KeyCode.Escape || Application.isPlaying)
                return;

            // Debug.Log("Escape key pressed - Handling global escape events...");

            // Debug.Log("Escape key pressed in Prefab Stage");
            // 只有在Prefab Stage中才檢查
            if (PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                _escapeCount = 0;
                Debug.Log("Escape key pressed but not in Prefab Stage, ignoring.");
                return;
            }

            // 防誤觸檢查：如果GUI控制項有焦點，不處理
            // if (GUIUtility.keyboardControl != 0)
            // {
            //     Debug.Log("Escape key pressed but GUI control has focus, ignoring."+GUIUtility.keyboardControl);
            //     _escapeCount = 0;
            //     return;
            // }

            double currentTime = EditorApplication.timeSinceStartup;

            // 如果超過0.4秒，重置計數
            if (currentTime - _lastEscapeTime > 0.4)
            {
                _escapeCount = 1;
            }
            else
            {
                _escapeCount++;
            }

            _lastEscapeTime = currentTime;

            // 連續按兩次ESC
            if (_escapeCount >= 2)
            {
                Debug.Log("Double ESC detected - Exiting Prefab Stage...");
                ExitPrefabStage();
                _escapeCount = 0;
                Event.current.Use(); // 消費掉這個事件
            }
        }
    }
}
