using UnityEditor;
using UnityEngine;

public class EditorCloseWindowTab : EditorWindow
{
    [MenuItem("RCGs/ShortCut/Close Window Tab %_W")]
    static void CloseTab()
    {
        EditorWindow focusedWindow = EditorWindow.focusedWindow;
        if (focusedWindow != null)
        {
            // Debug.Log(focusedWindow is MonoNodeWindow);
            // Debug.Log(focusedWindow.GetType());
            // if (focusedWindow is MonoNodeWindow)
                CloseTab(focusedWindow);
        }
        else
        {
            Debug.LogWarning("Found no focused window to close");
        }
    }

    static void CloseTab(EditorWindow editorWindow)
    {
        editorWindow.Close();
    }
}