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
            if (GUILayout.Button("Find Missing Scripts in selected GameObjects")) FindInSelected();
        }

        private static void FindInSelected()
        {
            var go = Selection.activeGameObject.GetComponentsInChildren<Transform>(true);
            int go_count = 0, components_count = 0, missing_count = 0;
            foreach (var g in go)
            {
                go_count++;
                var components = g.GetComponents<Component>();
                for (var i = 0; i < components.Length; i++)
                {
                    components_count++;
                    if (components[i] == null)
                    {
                        missing_count++;
                        var s = g.name;
                        var t = g.transform;
                        while (t.parent != null)
                        {
                            s = t.parent.name + "/" + s;
                            t = t.parent;
                        }

                        Debug.Log(s + " has an empty script attached in position: " + i, g);
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g.gameObject);
                        // DestroyImmediate(components[i]); // Uncomment this to also remove the missing script
                    }
                }
            }

            Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count,
                components_count, missing_count));
        }
    }
}