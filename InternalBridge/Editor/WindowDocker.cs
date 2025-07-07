using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.InternalBridge
{
    internal static class WindowDocker
    {
        [MenuItem("Window/General/Maximize Game View %#_.")]
        public static void MaximizeGameView()
        {
            var gameView = EditorWindow.GetWindow<GameView>();
            gameView.maximized = !gameView.maximized;
        }


        public static SceneHierarchyWindow GetSceneHierarchyWindow =>
            (SceneHierarchyWindow)EditorWindow.GetWindow(typeof(SceneHierarchyWindow));

        public enum DockPosition
        {
            Left,
            Top,
            Right,
            Bottom
        }

        private static Vector2 GetFakeMousePosition(EditorWindow wnd, DockPosition position)
        {
            var mousePosition = Vector2.zero;
            var viewPos = wnd.position;
            // The 20 is required to make the docking work.
            // Smaller values might not work when faking the mouse position.
            var offset = 100;
            switch (position)
            {
                case DockPosition.Left: mousePosition = new Vector2(offset, viewPos.size.y / 2); break;
                case DockPosition.Top: mousePosition = new Vector2(viewPos.size.x / 2, offset); break;
                case DockPosition.Right:
                    mousePosition = new Vector2(viewPos.size.x - offset, viewPos.size.y / 2); break;
                case DockPosition.Bottom:
                    mousePosition = new Vector2(viewPos.size.x / 2, viewPos.size.y - offset); break;
            }

            return new Vector2(viewPos.x + mousePosition.x, viewPos.y + mousePosition.y);
        }

        /// <summary>
        /// Docks the second window to the first window as a tab
        /// </summary>
        public static void AddTab(this EditorWindow wnd, EditorWindow other)
        {
            var dockArea = (DockArea)wnd.m_Parent;
            var childDockArea = (DockArea)other.m_Parent;
            childDockArea.RemoveTab(other);
            dockArea.AddTab(other);
        }

        public static void Dock(this EditorWindow wnd, EditorWindow other, DockPosition position)
        {
            try
            {
                // 使用目標視窗的頂層容器而不是視窗本身的位置
                var targetDockArea = wnd.m_Parent as DockArea;
                var sourceDockArea = other.m_Parent as DockArea;

                if (targetDockArea == null || sourceDockArea == null)
                {
                    Debug.LogError("Cannot find DockArea for windows");
                    wnd.AddTab(other); // Fallback to tab dock
                    return;
                }

                var containerWindow = targetDockArea.window;
                if (containerWindow == null)
                {
                    Debug.LogError("Container window is null");
                    wnd.AddTab(other);
                    return;
                }

                // 使用容器視窗的位置來計算滑鼠位置，而不是視窗本身
                var windowRect = containerWindow.position;
                var mousePosition = GetFakeMousePositionForContainer(windowRect, position);

                var splitView = containerWindow.rootSplitView;
                if (splitView == null)
                {
                    Debug.LogError("Root split view is null");
                    wnd.AddTab(other);
                    return;
                }

                Debug.Log($"Docking {other.GetType().Name} to {wnd.GetType().Name} at {position}");
                Debug.Log($"Container window rect: {windowRect}, Mouse position: {mousePosition}");

                // 設定原始拖拽來源
                DockArea.s_OriginalDragSource = sourceDockArea;

                // 嘗試獲取 dropInfo
                var dropInfo = splitView.DragOver(other, mousePosition);
                Debug.Log($"DropInfo: {dropInfo}");

                if (dropInfo == null)
                {
                    // 嘗試使用不同的滑鼠位置
                    var alternativePosition = GetAlternativeMousePosition(windowRect, position);
                    dropInfo = splitView.DragOver(other, alternativePosition);
                    Debug.Log($"Alternative position {alternativePosition}, DropInfo: {dropInfo}");

                    if (dropInfo != null)
                    {
                        mousePosition = alternativePosition;
                    }
                }

                if (dropInfo == null)
                {
                    Debug.LogWarning("DropInfo is still null, using Tab dock fallback");
                    wnd.AddTab(other);
                    return;
                }

                // 執行分割 dock
                splitView.PerformDrop(other, dropInfo, mousePosition);
                Debug.Log("Split dock successful");

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Dock operation failed: {ex.Message}\nStack trace: {ex.StackTrace}");
                // 最後備用方案：Tab dock
                try
                {
                    wnd.AddTab(other);
                    Debug.Log("Fallback Tab dock successful");
                }
                catch (System.Exception tabEx)
                {
                    Debug.LogError($"Even Tab dock failed: {tabEx.Message}");
                }
            }
        }

        private static Vector2 GetFakeMousePositionForContainer(Rect containerRect, DockPosition position)
        {
            var offset = 50f; // 使用較小的偏移量
            Vector2 localPosition;

            switch (position)
            {
                case DockPosition.Left:
                    localPosition = new Vector2(offset, containerRect.height / 2);
                    break;
                case DockPosition.Top:
                    localPosition = new Vector2(containerRect.width / 2, offset);
                    break;
                case DockPosition.Right:
                    localPosition = new Vector2(containerRect.width - offset, containerRect.height / 2);
                    break;
                case DockPosition.Bottom:
                    localPosition = new Vector2(containerRect.width / 2, containerRect.height - offset);
                    break;
                default:
                    localPosition = new Vector2(containerRect.width / 2, containerRect.height / 2);
                    break;
            }

            return new Vector2(containerRect.x + localPosition.x, containerRect.y + localPosition.y);
        }

        private static Vector2 GetAlternativeMousePosition(Rect containerRect, DockPosition position)
        {
            var offset = 100f; // 使用更大的偏移量作為備用方案
            Vector2 localPosition;

            switch (position)
            {
                case DockPosition.Left:
                    localPosition = new Vector2(offset, containerRect.height * 0.3f);
                    break;
                case DockPosition.Top:
                    localPosition = new Vector2(containerRect.width * 0.3f, offset);
                    break;
                case DockPosition.Right:
                    localPosition = new Vector2(containerRect.width - offset, containerRect.height * 0.3f);
                    break;
                case DockPosition.Bottom:
                    localPosition = new Vector2(containerRect.width * 0.3f, containerRect.height - offset);
                    break;
                default:
                    localPosition = new Vector2(containerRect.width * 0.3f, containerRect.height * 0.3f);
                    break;
            }

            return new Vector2(containerRect.x + localPosition.x, containerRect.y + localPosition.y);
        }
        

        /// <summary>
        /// Sets the size of the specified EditorWindow
        /// 設定指定 EditorWindow 的大小
        /// </summary>
        public static void SetWindowSize(this EditorWindow window, float width, float height)
        {
            var currentPos = window.position;
            var newRect = new Rect(currentPos.x, currentPos.y, width, height);
            window.position = newRect;

            // 強制重繪以確保變更生效
            window.Repaint();

            Debug.Log($"Window {window.GetType().Name} size set to {width}x{height}");
        }

        /// <summary>
        /// Sets the position of the specified EditorWindow
        /// 設定指定 EditorWindow 的位置
        /// </summary>
        public static void SetWindowPosition(this EditorWindow window, float x, float y)
        {
            var currentPos = window.position;
            var newRect = new Rect(x, y, currentPos.width, currentPos.height);
            window.position = newRect;

            // 強制重繪以確保變更生效
            window.Repaint();

            Debug.Log($"Window {window.GetType().Name} position set to ({x}, {y})");
        }

        /// <summary>
        /// Sets both size and position of the specified EditorWindow
        /// 同時設定指定 EditorWindow 的大小和位置
        /// </summary>
        public static void SetWindowRect(this EditorWindow window, float x, float y, float width, float height)
        {
            var newRect = new Rect(x, y, width, height);
            window.position = newRect;

            // 強制重繪以確保變更生效
            window.Repaint();

            Debug.Log($"Window {window.GetType().Name} rect set to ({x}, {y}, {width}, {height})");
        }

        /// <summary>
        /// Gets the current position and size of the specified EditorWindow
        /// 獲取指定 EditorWindow 的當前位置和大小
        /// </summary>
        public static Rect GetWindowRect(this EditorWindow window)
        {
            return window.position;
        }

        /// <summary>
        /// Centers the window on the screen
        /// 將視窗置中到螢幕
        /// </summary>
        public static void CenterWindow(this EditorWindow window)
        {
            var currentPos = window.position;
            var screenWidth = Screen.currentResolution.width;
            var screenHeight = Screen.currentResolution.height;

            var newX = (screenWidth - currentPos.width) / 2;
            var newY = (screenHeight - currentPos.height) / 2;

            window.SetWindowPosition(newX, newY);

            Debug.Log($"Window {window.GetType().Name} centered at ({newX}, {newY})");
        }

        /// <summary>
        /// Resizes window to fit content with minimum size constraints
        /// 調整視窗大小以適應內容，並套用最小尺寸限制
        /// </summary>
        public static void OptimizeWindowSize(this EditorWindow window, float minWidth = 300, float minHeight = 200)
        {
            var currentPos = window.position;
            var newWidth = Mathf.Max(currentPos.width, minWidth);
            var newHeight = Mathf.Max(currentPos.height, minHeight);

            window.SetWindowSize(newWidth, newHeight);

            Debug.Log($"Window {window.GetType().Name} optimized to {newWidth}x{newHeight}");
        }

        /// <summary>
        /// Resets Unity Editor layout to default using direct WindowLayout API
        /// 使用直接的 WindowLayout API 重設 Unity Editor 布局到預設狀態
        /// </summary>
        public static bool ResetEditorLayout()
        {
            try
            {
                WindowLayout.LoadCurrentModeLayout(true);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to reset Unity Editor layout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets all Unity Editor layouts to factory defaults
        /// 重設所有 Unity Editor 布局為工廠預設值
        /// </summary>
        public static bool ResetAllLayouts()
        {
            try
            {
                WindowLayout.ResetAllLayouts(false);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to reset all Unity Editor layouts: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets to factory settings
        /// 重設為工廠設定
        /// </summary>
        public static bool ResetFactorySettings()
        {
            try
            {
                WindowLayout.ResetFactorySettings();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to reset factory settings: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Save current layout for current mode
        /// 為當前模式保存當前布局
        /// </summary>
        public static bool SaveCurrentLayoutPerMode(string modeId)
        {
            try
            {
                WindowLayout.SaveCurrentLayoutPerMode(modeId);
                Debug.Log($"Unity Editor layout saved for mode '{modeId}'");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save Unity Editor layout for mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load Unity Editor layout with direct WindowLayout API
        /// 使用直接的 WindowLayout API 載入 Unity Editor 布局
        /// </summary>
        public static bool LoadEditorLayout(string layoutPath)
        {
            try
            {
                bool success = WindowLayout.TryLoadWindowLayout(layoutPath, false);
                if (success)
                {
                    Debug.Log($"Unity Editor layout loaded: '{layoutPath}'");
                }
                else
                {
                    Debug.LogWarning($"Failed to load Unity Editor layout: '{layoutPath}'");
                }
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load Unity Editor layout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load Unity Editor layout with detailed flags
        /// 使用詳細旗標載入 Unity Editor 布局
        /// </summary>
        public static bool LoadEditorLayout(string layoutPath, bool newProjectLayoutWasCreated)
        {
            try
            {
                bool success = WindowLayout.TryLoadWindowLayout(layoutPath, newProjectLayoutWasCreated);
                if (success)
                {
                    Debug.Log($"Unity Editor layout loaded: '{layoutPath}'");
                }
                else
                {
                    Debug.LogWarning($"Failed to load Unity Editor layout: '{layoutPath}'");
                }
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load Unity Editor layout: {ex.Message}");
                return false;
            }
        }
        

        /// <summary>
        /// Load default Unity Editor layout
        /// 載入預設 Unity Editor 布局
        /// </summary>
        public static bool LoadDefaultLayout()
        {
            try
            {
                WindowLayout.LoadDefaultLayout();
                Debug.Log("Default Unity Editor layout loaded");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load default Unity Editor layout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load layout from file dialog
        /// 從檔案對話方塊載入布局
        /// </summary>
        public static void LoadLayoutFromFile()
        {
            try
            {
                WindowLayout.LoadFromFile();
                Debug.Log("Layout loaded from file");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load layout from file: {ex.Message}");
            }
        }

        /// <summary>
        /// Save layout to file dialog
        /// 儲存布局到檔案對話方塊
        /// </summary>
        public static void SaveLayoutToFile()
        {
            try
            {
                WindowLayout.SaveToFile();
                Debug.Log("Layout saved to file");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save layout to file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current Unity Editor layout information
        /// 獲取當前 Unity Editor 布局資訊
        /// FIXME: 閃退！
        /// </summary>
        public static LayoutInfo GetCurrentLayoutInfo()
        {
            var layoutInfo = new LayoutInfo();

            try
            {
                // 獲取所有 ContainerWindow
                var containerWindows = ContainerWindow.windows;
                layoutInfo.WindowCount = containerWindows?.Length ?? 0;

                foreach (var window in containerWindows ?? new ContainerWindow[0])
                {
                    var windowInfo = new WindowInfo
                    {
                        Position = window.position,
                        IsMainWindow = window.showMode == ShowMode.MainWindow
                    };

                    // 獲取視窗中的所有視圖
                    if (window.rootView != null)
                    {
                        CollectViewInfo(window.rootView, windowInfo.Views);
                    }

                    layoutInfo.Windows.Add(windowInfo);
                }

                // 獲取當前布局名稱
                layoutInfo.CurrentLayoutName = GetCurrentLayoutName();

                Debug.Log($"Collected layout info: {layoutInfo.WindowCount} windows, current layout: {layoutInfo.CurrentLayoutName}");
                return layoutInfo;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get layout info: {ex.Message}");
                layoutInfo.Error = ex.Message;
                return layoutInfo;
            }
        }

        /// <summary>
        /// Get current layout path
        /// 獲取當前布局路徑
        /// </summary>
        public static string GetCurrentLayoutPath()
        {
            try
            {
                return WindowLayout.GetCurrentLayoutPath();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get current layout path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get default layout path
        /// 獲取預設布局路徑
        /// </summary>
        public static string GetDefaultLayoutPath()
        {
            try
            {
                return WindowLayout.GetDefaultLayoutPath();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get default layout path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Show window with dynamic layout from JSON file
        /// 從 JSON 檔案使用動態布局顯示視窗
        /// </summary>
        public static ContainerWindow ShowWindowWithDynamicLayout(string windowId, string layoutDataPath)
        {
            try
            {
                var window = WindowLayout.ShowWindowWithDynamicLayout(windowId, layoutDataPath);
                if (window != null)
                {
                    Debug.Log($"Window '{windowId}' shown with dynamic layout from '{layoutDataPath}'");
                }
                else
                {
                    Debug.LogWarning($"Failed to show window '{windowId}' with dynamic layout");
                }
                return window;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to show window with dynamic layout: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find main window
        /// 尋找主視窗
        /// </summary>
        public static ContainerWindow FindMainWindow()
        {
            try
            {
                return WindowLayout.FindMainWindow();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to find main window: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Maximize editor window
        /// 最大化編輯器視窗
        /// </summary>
        public static bool MaximizeWindow(EditorWindow window)
        {
            try
            {
                if (WindowLayout.IsMaximized(window))
                {
                    Debug.Log($"Window {window.GetType().Name} is already maximized");
                    return true;
                }
                
                WindowLayout.Maximize(window);
                Debug.Log($"Window {window.GetType().Name} maximized");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to maximize window: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unmaximize editor window
        /// 取消最大化編輯器視窗
        /// </summary>
        public static bool UnmaximizeWindow(EditorWindow window)
        {
            try
            {
                if (!WindowLayout.IsMaximized(window))
                {
                    Debug.Log($"Window {window.GetType().Name} is not maximized");
                    return true;
                }
                
                WindowLayout.Unmaximize(window);
                Debug.Log($"Window {window.GetType().Name} unmaximized");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to unmaximize window: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if window is maximized
        /// 檢查視窗是否已最大化
        /// </summary>
        public static bool IsWindowMaximized(EditorWindow window)
        {
            try
            {
                return WindowLayout.IsMaximized(window);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to check if window is maximized: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get maximized window
        /// 獲取最大化的視窗
        /// </summary>
        public static EditorWindow GetMaximizedWindow()
        {
            try
            {
                return WindowLayout.GetMaximizedWindow();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get maximized window: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Close all windows
        /// 關閉所有視窗
        /// </summary>
        public static void CloseAllWindows()
        {
            try
            {
                WindowLayout.CloseWindows();
                Debug.Log("All windows closed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to close all windows: {ex.Message}");
            }
        }

        /// <summary>
        /// Find editor window of specific type
        /// 尋找特定類型的編輯器視窗
        /// </summary>
        public static EditorWindow FindEditorWindowOfType(System.Type type)
        {
            try
            {
                Debug.Log($"Finding editor window of type: {type?.Name}");
                return WindowLayout.FindEditorWindowOfType(type);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to find editor window of type {type?.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find editor window of specific type (generic version)
        /// 尋找特定類型的編輯器視窗（泛型版本）
        /// </summary>
        public static T FindEditorWindowOfType<T>() where T : EditorWindow
        {
            try
            {
                return WindowLayout.FindEditorWindowOfType(typeof(T)) as T;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to find editor window of type {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        private static void CollectViewInfo(View view, System.Collections.Generic.List<ViewInfo> viewList)
        {
            if (view == null) return;

            var viewInfo = new ViewInfo
            {
                ViewType = view.GetType().Name,
                Position = view.position
            };

            // 如果是 DockArea，獲取其中的 EditorWindow 資訊
            if (view is DockArea dockArea)
            {
                viewInfo.WindowTypes = new System.Collections.Generic.List<string>();
                foreach (var window in dockArea.m_Panes)
                {
                    if (window != null)
                    {
                        viewInfo.WindowTypes.Add(window.GetType().Name);
                    }
                }
                viewInfo.ActiveWindowIndex = dockArea.selected;
            }

            viewList.Add(viewInfo);

            // 遞歸處理子視圖
            foreach (var child in view.allChildren)
            {
                CollectViewInfo(child, viewList);
            }
        }

        private static string GetCurrentLayoutName()
        {
            try
            {
                return WindowLayout.currentLayoutName ?? "Unknown";
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to get current layout name: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Get available layout names from last layouts
        /// 從最後使用的布局中獲取可用的布局名稱
        /// </summary>
        public static string[] GetAvailableLayouts()
        {
            try
            {
                var lastLayouts = WindowLayout.GetLastLayout();
                var layoutNames = new System.Collections.Generic.List<string>();
                
                foreach (var layoutPath in lastLayouts)
                {
                    if (!string.IsNullOrEmpty(layoutPath))
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(layoutPath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            layoutNames.Add(fileName);
                            Debug.Log($"Found layout: {fileName} at {layoutPath}");
                        }
                    }
                }
                
                Debug.Log($"Found {layoutNames.Count} available layouts");
                return layoutNames.ToArray();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get available layouts: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Get layout paths with specified directory, mode and version
        /// 使用指定的目錄、模式和版本獲取布局路徑
        /// </summary>
        public static string[] GetLayoutPaths(string directory, string mode, int version)
        {
            try
            {
                var layoutPaths = WindowLayout.GetLastLayout(directory, mode, version);
                return layoutPaths.ToArray();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get layout paths: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Get layout resources path
        /// 獲取布局資源路徑
        /// </summary>
        public static string GetLayoutResourcesPath()
        {
            try
            {
                return WindowLayout.layoutResourcesPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get layout resources path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get layout preferences path
        /// 獲取布局偏好設定路徑
        /// </summary>
        public static string GetLayoutPreferencesPath()
        {
            try
            {
                return WindowLayout.layoutsPreferencesPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get layout preferences path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get layout mode preferences path
        /// 獲取布局模式偏好設定路徑
        /// </summary>
        public static string GetLayoutModePreferencesPath()
        {
            try
            {
                return WindowLayout.layoutsModePreferencesPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get layout mode preferences path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get layout project path
        /// 獲取布局專案路徑
        /// </summary>
        public static string GetLayoutProjectPath()
        {
            try
            {
                return WindowLayout.layoutsProjectPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get layout project path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get project layout path for specific mode
        /// 獲取特定模式的專案布局路徑
        /// </summary>
        public static string GetProjectLayoutPerMode(string modeId)
        {
            try
            {
                return WindowLayout.GetProjectLayoutPerMode(modeId);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to get project layout per mode: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 布局資訊結構
    /// </summary>
    public class LayoutInfo
    {
        public int WindowCount { get; set; }
        public string CurrentLayoutName { get; set; } = "Unknown";
        public System.Collections.Generic.List<WindowInfo> Windows { get; set; } = new System.Collections.Generic.List<WindowInfo>();
        public string Error { get; set; }
    }

    /// <summary>
    /// 視窗資訊結構
    /// </summary>
    public class WindowInfo
    {
        public Rect Position { get; set; }
        public bool IsMainWindow { get; set; }
        public System.Collections.Generic.List<ViewInfo> Views { get; set; } = new System.Collections.Generic.List<ViewInfo>();
    }

    /// <summary>
    /// 視圖資訊結構
    /// </summary>
    public class ViewInfo
    {
        public string ViewType { get; set; }
        public Rect Position { get; set; }
        public System.Collections.Generic.List<string> WindowTypes { get; set; }
        public int ActiveWindowIndex { get; set; } = -1;
    }
}