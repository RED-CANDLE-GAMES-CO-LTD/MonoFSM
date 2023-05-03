#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCGMaker.Core
{
    public static class AssetDatabaseExtension
    {
        public static string GetGUID(this Object obj)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId);
            return guid;
        }

        public static GameFlagBase CreateGameStateSO(this System.Type type, string fileRelativePath,
            MonoBehaviour refObj)
        {
            var gameStateSo = CreateScriptableObject(type, fileRelativePath) as GameFlagBase;
            if (gameStateSo == null)
            {
                Debug.LogError("Create Scriptable Object Failed", refObj);
                return null;
            }

            if (!refObj.TryGetComponent<AutoGenGameState>(out var autoGenGameState)) return gameStateSo;

            //自動生成的，SaveID另外做
            gameStateSo.type = GameFlagBase.GameStateType.AutoUnique;
            gameStateSo.SaveID = autoGenGameState.SaveID;

            return gameStateSo;
        }

        public static ScriptableObject CreateScriptableObject(this System.Type type, string fileRelativePath)
        {
            var folderRelativePath = fileRelativePath.FolderPath();
            Debug.Log("Want to Create Asset at: Assets/" + fileRelativePath);
            if (!AssetDatabase.IsValidFolder("Assets/" + folderRelativePath))
            {
                Debug.Log("Create Folder: Assets/" + folderRelativePath);
                CreateFolderAtPath(folderRelativePath);
            }


            //check if file exist
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/" + fileRelativePath);
            if (asset != null)
            {
                Debug.LogWarning("File already exist, linked");
                return asset;
            }

            asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, "Assets/" + fileRelativePath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static void CreateFolderAtPath(string folderPath)
        {
            var folders = folderPath.Split('/');
            var currentPath = "Assets";

            for (var i = 0; i < folders.Length; i++)
            {
                var folder = folders[i];

                if (!string.IsNullOrEmpty(folder))
                {
                    var newPath = currentPath + "/" + folder;

                    if (!AssetDatabase.IsValidFolder(newPath)) AssetDatabase.CreateFolder(currentPath, folder);

                    currentPath = newPath;
                }
            }
        }
    }
}
#endif