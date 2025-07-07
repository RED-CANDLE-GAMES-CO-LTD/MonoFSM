using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace MonoFSM.InternalBridge
{
    // internal static class InternalEngineBridge
    // {
    //     // Note how NumericFieldDraggerUtility is an internal Unity class.
    //     public static float NiceDelta(Vector2 deviceDelta, float acceleration) => 
    //         NumericFieldDraggerUtility.NiceDelta(deviceDelta, acceleration);
    // }
    
    internal static class BridgeEditorUtility
    {
        public static void RepaintAllViews()
        {
            // 官方未公開 API，但在 Editor 中可安全呼叫
            InternalEditorUtility.RepaintAllViews();
        }
    }

    //Editor Tool要搬到MCP Server嗎？
    internal static class ScreenShotUtility
    {


        public static void ScreenshotCurrentView()
        {
            // ScreenShots.SetMainWindowSizeSmall(); 
            ScreenShots.Screenshot();
        }

        public static void ScreenGameViewContent()
        {
            ScreenShots.ScreenGameViewContent();
        }

        public static MainView GetMainView()
        {
            // This is a workaround to get the MainView, which is not directly accessible.
            // It uses the Resources.FindObjectsOfTypeAll method to find the MainView instance.
            return Resources.FindObjectsOfTypeAll<MainView>()[0];
        }
        public static bool GetMainContainerWindow(string filePath)
        {
            // This is a workaround to get the MainContainerWindow, which is not directly accessible.
            // It uses the Resources.FindObjectsOfTypeAll method to find the MainContainerWindow instance.
            // var view = GetMainView();
            // Debug.Log("Position of MainView: " + view.position.position + "Screen Position: " + view.screenPosition.position+" Window Position: " + view.windowPosition.position);
            // var screenPosition = view.screenPosition.position * EditorGUIUtility.pixelsPerPoint;

            var activeWindow = GetMainView().window;

            //必定會被toolbar佔掉？因為已經滿了？
            //activeWindow.position.position* EditorGUIUtility.pixelsPerPoint 
            // var vec2Position = screenPosition;// * EditorGUIUtility.pixelsPerPoint;
            var vec2Position = activeWindow.position.position * EditorGUIUtility.pixelsPerPoint;
            // float tabsHeight = 20f; // 假設有一個_tabsHeight的高度偏移
            // vec2Position += new Vector2(0, tabsHeight); // 假設有一個_tabsHeight的高度偏移
            var sizeX = (int)(activeWindow.position.width * EditorGUIUtility.pixelsPerPoint) + 100;// +(int)screenPosition.x;
            var sizeY = (int)(activeWindow.position.height * EditorGUIUtility.pixelsPerPoint) + (int)vec2Position.y;
            UnityEngine.Debug.Log("Window pos?:" + activeWindow.position.position);
            Debug.Log("Ori Window Size: " + activeWindow.position.size + " Screen Position: " + vec2Position + " Size: " + sizeX + "x" + sizeY);
            Debug.Log($"Taking screenshot of window: {activeWindow.name}, Size: {sizeX}x{sizeY}, Position: {vec2Position}");

            // 使用Unity的內部方法讀取螢幕像素
            var colors = InternalEditorUtility.ReadScreenPixel(new Vector2(100, 0), sizeX, sizeY);

            if (colors == null || colors.Length == 0)
            {
                Debug.LogError("Failed to read screen pixels");
                return false;
            }

            // 創建Texture2D並設置像素
            var result = new Texture2D(sizeX, sizeY, TextureFormat.RGB24, false);
            result.SetPixels(colors);
            result.Apply();

            // 編碼為PNG
            var bytes = result.EncodeToPNG();

            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError("Failed to encode PNG");
                UnityEngine.Object.DestroyImmediate(result);
                return false;
            }

            // 釋放記憶體
            UnityEngine.Object.DestroyImmediate(result);

            // 確保目錄存在
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 寫入檔案
            File.WriteAllBytes(filePath, bytes);

            // 驗證檔案是否正確寫入
            if (File.Exists(filePath) && new FileInfo(filePath).Length > 1000)
            {
                Debug.Log($"Screenshot successfully saved to: {filePath}");
                return true;
            }
            else
            {
                Debug.LogError("Screenshot file was not created properly");
                return false;
            }
        }
    }

    internal static class SceneHierarchyUtility
    {
        public static void ExpandHierarchyItem(GameObject gObj)
        {
            WindowDocker.GetSceneHierarchyWindow.SetExpandedRecursive(gObj.GetInstanceID(), true);
            Selection.activeGameObject = gObj;
        }

        public static void TryRepaintHierarchy()
        {
            if (SceneHierarchyWindow.lastInteractedHierarchyWindow &&
                SceneHierarchyWindow.lastInteractedHierarchyWindow.IsSelectedTab())
            {
                Debug.Log("Repainting Scene Hierarchy Window");
                //FIXME: repaint this frame?
                EditorApplication.RepaintHierarchyWindow();
            }

            // if (EditorWindow.HasOpenInstances<SceneHierarchyWindow>()) 
        }
        
        //FIXME: 會中斷Selection，有點buggy
        public static void RepaintInspector()
        {
            if (EditorWindow.HasOpenInstances<InspectorWindow>())
            {
                var inspectorWindow = EditorWindow.GetWindow<InspectorWindow>();
                inspectorWindow.Repaint();
            }
        }

    }


}