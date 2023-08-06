
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RCGMaker.Core
{
    public static class AssetDatabaseExtension
    {
        [EditorOnly]
        public static void SetDirty(this Object obj)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        //"Resources/Configs"
        [EditorOnly]
        public static void AssetInFolderValidate(this ScriptableObject asset, string folderName,
            SelfValidationResult result)
        {
#if UNITY_EDITOR
            //check if asset is in Resources/Config
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (!assetPath.Contains(folderName))
                result.AddError($"ScriptableObject {asset} should be in " + folderName).WithFix(() =>
                {
                    //move asset to Resources/Config
                    var newPath = assetPath.Replace("Assets/", "Assets/" + folderName + "/");
                    Debug.Log("Move SO To:" + newPath);
                    var moveResult = AssetDatabase.MoveAsset(assetPath, newPath);
                    if (moveResult != "")
                        Debug.LogError("Move Result:" + moveResult);
                    // AssetDatabase.Refresh();
                });
#endif
        }
#if UNITY_EDITOR
        public static string GetGUID(this Object obj)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId);
            return guid;
        }

        public static GameFlagBase CreateGameStateSO(this System.Type type, MonoBehaviour refObj,
            string subFolderName = "")
        {
            //遊戲中不該建state
            if (Application.isPlaying)
                return null;
            if (!refObj.TryGetComponent<AutoGenGameState>(out var autoGenGameState))
            {
                //不是自動生的
                var gameStateSo =
                    CreateScriptableObject(type, GameStateAttribute.GetFullPath(refObj.gameObject, subFolderName)) as
                        GameFlagBase;
                if (gameStateSo == null)
                {
                    Debug.LogError("Create Scriptable Object Failed", refObj);
                    return null;
                }

                return gameStateSo;
            }
            else
            {
                var folderRelativePath = GameStateAttribute.GetRelativePath(refObj.gameObject, subFolderName, true);
                var fileName = GameStateAttribute.GetFileName(refObj.gameObject) + autoGenGameState.MyGuid + ".asset";
                var gameStateSo =
                    CreateScriptableObject(type, folderRelativePath + "/" + fileName) as GameFlagBase;

                //自動生成的，SaveID另外做
                if (gameStateSo != null)
                {
                    gameStateSo.gameStateType = GameFlagBase.GameStateType.AutoUnique;
                    gameStateSo.SaveID = autoGenGameState.SaveID;
                    Debug.Log("Assign SaveID for autoGen", refObj);

                    return gameStateSo;
                }

                Debug.LogError("Create gameStateSo Auto Object Failed", refObj);
                return null;
            }
        }


        //單純給任何scriptable object用
        public static ScriptableObject CreateScriptableObject(this System.Type type, string fileRelativePath)
        {
            CreateAssetFolderIfParentNotExist(fileRelativePath);
            //check if file exist
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/" + fileRelativePath);
            if (asset != null)
            {
                Debug.Log("File already exist, linked" + fileRelativePath);
                return asset;
            }

            asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, "Assets/" + fileRelativePath);
            //[]: 這個不call OK 嗎？
            // AssetDatabase.SaveAssets();
            return asset;
        }

        [EditorOnly]
        private static void CreateAssetFolderIfParentNotExist(string fileRelativePath)
        {
            var folderRelativePath = fileRelativePath.FolderPath();
            // Debug.Log("Want to Create Asset at: Assets/" + fileRelativePath);
            if (!AssetDatabase.IsValidFolder("Assets/" + folderRelativePath))
            {
                // Debug.Log("Create Folder: Assets/" + folderRelativePath);
                CreateAssetFolderAtPathRecursive(folderRelativePath);
            }
        }

        [EditorOnly]
        private static void CreateAssetFolderAtPathRecursive(string folderPath) //一層一層往下建立資料夾
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
#endif
    }
}
