using UnityEngine;

namespace HierarchyIDEWindow.MonoFSM_HierarchyDrawer.Editor
{
    public static class IDELikeSearchbar
    {
        private static string searchFieldText;

        // public static WrappedEvent curEvent => _curEvent ??= typeof(Event).GetFieldValue<Event>("s_Current").Wrap();
        public static void SearchField(Rect searchFieldRect)
        {
            var currentEvt = Event.current;
            if (currentEvt.type == EventType.KeyDown)
            {
                // Event.current.keyCode
                // Handle keyboard events
                if (GUI.GetNameOfFocusedControl() == "SearchFilter")
                {
                    // Debug.Log("Search field is focused");
                }

                if (currentEvt.type == EventType.KeyDown)
                    switch (currentEvt.keyCode)
                    {
                        case KeyCode.Escape:
                            // Handle Escape key
                            // Debug.Log("Escape key pressed in search field");
                            // window.InvokeMethod("ClearSearchFilter");
                            // GUIUtility.keyboardControl = 0;
                            searchFieldText = "";
                            HierarchyHighLightEditor.FilterObjects("");
                            currentEvt.Use();
                            break;

                        case KeyCode.UpArrow:
                            // Handle Up arrow key
                            // Debug.Log("Up arrow key pressed in search field");
                            HierarchyHighLightEditor.FindPreviousObject();
                            currentEvt.Use();
                            break;
                        case KeyCode.Return:
                        case KeyCode.Tab:
                        case KeyCode.DownArrow:
                            // Handle Down arrow key
                            // Debug.Log("Down arrow key pressed in search field");
                            HierarchyHighLightEditor.FindNextObject();
                            currentEvt.Use();
                            break;
                    }
            }

            GUI.SetNextControlName("SearchFilter");
            searchFieldText = GUILayout.TextField(searchFieldText, "ToolbarSearchTextField",
                GUILayout.MinWidth(30), GUILayout.MaxWidth(searchFieldRect.width - 30));

            HierarchyHighLightEditor.FilterObjects(searchFieldText);
        }
    }
}