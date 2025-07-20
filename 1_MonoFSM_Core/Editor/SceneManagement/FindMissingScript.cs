namespace EditorTool
{
    using UnityEngine;
    using UnityEditor;

    public class FindMissingScripts : EditorWindow
    {
        [MenuItem("Window/FindMissingScripts")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FindMissingScripts));
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Find Missing Scripts under selected GameObjects")) FindInSelected();
        }

        private static void FindInSelected()
        {
            var go = Selection.activeGameObject.GetComponentsInChildren<Transform>(true);
            int go_count = 0, components_count = 0, missing_count = 0;
            foreach (var g in go)
            {
                go_count++;
                var result = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g.gameObject);
                if (result > 0)
                {
                    missing_count += result;
                    Debug.Log(string.Format("GameObject {0} has {1} missing scripts", g.name, result), g);
                }
                
            }

            Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count,
                components_count, missing_count));
        }
    }
}