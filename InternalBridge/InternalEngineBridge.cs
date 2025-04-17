using UnityEditor;
using UnityEngine;

namespace MonoFSM.InternalBridge
{
    // internal static class InternalEngineBridge
    // {
    //     // Note how NumericFieldDraggerUtility is an internal Unity class.
    //     public static float NiceDelta(Vector2 deviceDelta, float acceleration) => 
    //         NumericFieldDraggerUtility.NiceDelta(deviceDelta, acceleration);
    // }

    internal static class WindowDocker
    {
        public static SceneHierarchyWindow GetSceneHierarchyWindow => (SceneHierarchyWindow)EditorWindow.GetWindow(typeof(SceneHierarchyWindow));

        public enum DockPosition
        {
            Left,
            Top,
            Right,
            Bottom
        }

        private static Vector2 GetFakeMousePosition(EditorWindow wnd, DockPosition position)
        {
            Vector2 mousePosition = Vector2.zero;
            var viewPos = wnd.position;
            // The 20 is required to make the docking work.
            // Smaller values might not work when faking the mouse position.
            var offset = 100;
            switch(position)
            {
                case DockPosition.Left: mousePosition = new Vector2(offset,viewPos.size.y / 2); break;
                case DockPosition.Top: mousePosition = new Vector2(viewPos.size.x / 2, offset); break;
                case DockPosition.Right: mousePosition = new Vector2(viewPos.size.x - offset, viewPos.size.y / 2); break;
                case DockPosition.Bottom: mousePosition = new Vector2(viewPos.size.x / 2,viewPos.size.y - offset); break;
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
            var mousePosition = GetFakeMousePosition(wnd, position);
            var dockArea = wnd.m_Parent as DockArea;
            var containerWindow = dockArea.window;
            SplitView splitView = containerWindow.rootSplitView;
            Debug.Log("Docking " + other + " to " + wnd + " at " + position);
            var dropInfo = splitView.DragOver(other, mousePosition);
            Debug.Log("DropInfo: " + dropInfo);
            DockArea.s_OriginalDragSource = (DockArea)other.m_Parent;
            splitView.PerformDrop(other, dropInfo, mousePosition);
        
        }
        // public static void DockTo(this EditorWindow first, EditorWindow second, DockPosition position)
        // {
        //     Vector2 mousePosition = GetFakeMousePosition(second, position);
        //     Debug.Log("Fake mouse position: " + mousePosition);
        //     SplitView targetView = null;
        //     DropInfo dropInfo = null;
        //     var windows = ContainerWindow.windows;
        //     Debug.Log("Found " + windows.Length + " windows.");
        //     for (int i = 0; i < windows.Length; i++)
        //     {
        //         SplitView rootSplitView = windows[i].rootSplitView;
        //         if (rootSplitView != null)
        //         {
        //             dropInfo = rootSplitView.DragOverRootView(mousePosition);
        //             targetView = rootSplitView;
        //             Debug.Log("Found rootSplitView: " + rootSplitView);
        //         }
        //
        //         if (dropInfo == null)
        //         {
        //            
        //             View rootView = windows[i].rootView;
        //             Debug.Log("No rootSplitView found, checking rootView:"+rootView.name);
        //             foreach (var view in rootView.allChildren)
        //             {
        //                 Debug.Log("Checking view: " + view);
        //                 if (view is not IDropArea dropArea) continue;
        //                 dropInfo = dropArea.DragOver(second, mousePosition);
        //                 if (dropInfo == null) continue;
        //                 Debug.Log("Found dropInfo: " + dropInfo);
        //                 targetView = view as SplitView;
        //                 if (targetView == null)
        //                 {
        //                     Debug.LogError("Target view is null."+view.name);
        //                 }
        //                 else
        //                     break;
        //             }
        //         }
        //         else
        //         {
        //             Debug.Log("Found dropInfo: " + dropInfo);
        //             break;
        //         }
        //         
        //         if (targetView != null && dropInfo != null)
        //         {
        //             DockArea.s_OriginalDragSource = (DockArea) first.m_Parent;
        //             Debug.Log("Docked " + first + " to " + second + " at " + position);
        //             targetView.PerformDrop(first, dropInfo, mousePosition);
        //             
        //             break;
        //         }
        //     }
        //     if(dropInfo == null)
        //     {
        //         Debug.LogError("No dropInfo found for docking.");
        //         return;
        //     }
        //
        //     
        // }
    }
    
}