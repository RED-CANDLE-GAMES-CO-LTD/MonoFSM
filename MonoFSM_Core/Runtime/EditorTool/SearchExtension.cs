using System.Reflection;
using UnityEditor;
using UnityEngine;


public static class SearchExtension
{
    public static void SetFilterForAssignPrefab(this Component go)
    {
#if UNITY_EDITOR
        SetSearchFilter("t:effectDealer,t:fxplayer,t:shootSpawner", 0);
#endif
    }

    public static void SetSearchFilter(string filter, int filterMode)
    {
#if UNITY_EDITOR
        SearchableEditorWindow hierarchy;
        var windows = (SearchableEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(SearchableEditorWindow));

        foreach (var window in windows)
            if (window.GetType().ToString() == "UnityEditor.SceneHierarchyWindow")
            {
                hierarchy = window;
                if (hierarchy == null)
                    return;

                var setSearchType = typeof(SearchableEditorWindow).GetMethod("SetSearchFilter",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var parameters = new object[] { filter, filterMode, false, false };

                setSearchType.Invoke(hierarchy, parameters);
                break;
            }
#endif
    }
}