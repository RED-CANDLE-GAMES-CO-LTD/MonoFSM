using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

public class ApplicationCoreUtils
{
#if UNITY_EDITOR
    [MenuItem("Tools/MonoFSM/Copy ApplicationCore to Custom")]

    public static void CopyApplicationCore()
    {
        // Try to find the ApplicationCore prefab
        // We prioritize the one in Assets if it exists, otherwise look in packages/modules
        string[] guids = AssetDatabase.FindAssets("ApplicationCore t:Prefab");
        string sourcePath = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path) == "ApplicationCore.prefab")
            {
                // Prefer the one in Resources/Configs if possible
                if (path.Contains("Resources/Configs"))
                {
                    sourcePath = path;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(sourcePath))
        {
            Debug.LogError("Could not find 'ApplicationCore.prefab'. Please make sure it exists.");
            return;
        }

        string directory = Path.GetDirectoryName(sourcePath);
        string destinationPath = Path.Combine(directory, "ApplicationCore_Custom.prefab");

        // If destination already exists, unique name or warn?
        // User asked for "ApplicationCore_Custom", so we check that specifically.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null)
        {
            Debug.LogWarning($"'ApplicationCore_Custom.prefab' already exists at {destinationPath}. Aborting copy.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath));
            return;
        }

        if (AssetDatabase.CopyAsset(sourcePath, destinationPath))
        {
            Debug.Log($"Successfully copied '{sourcePath}' to '{destinationPath}'");
            AssetDatabase.Refresh();
            GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath);
            Selection.activeObject = newPrefab;
            EditorGUIUtility.PingObject(newPrefab);
        }
        else
        {
            Debug.LogError($"Failed to copy '{sourcePath}' to '{destinationPath}'");
        }
    }
#endif
}
