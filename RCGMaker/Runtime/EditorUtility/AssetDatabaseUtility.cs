using System;
using System.IO;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
#endif

using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core
{
    public static class AssetDatabaseUtility
    {
#if UNITY_EDITOR

        public delegate T AssetCreateDelegate<out T>(string prefabPath) where T : UnityEngine.Object;
        //FIXME: 全世界都用這個！
        //把目標asset複製到prefab所在的資料夾
        public static T CopyAssetOrCreateToPrefabFolder<T>(T oriAsset,string assetExtension, AssetCreateDelegate<T> customAssetCreationMethod) where T : UnityEngine.Object
        {
           var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
           if (prefabStage == null)
           {
               Debug.LogError("Not in prefab stage");
               return null;
           }
           var prefabPath  = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
           var prefabFolderPath = Path.GetDirectoryName(prefabPath );
           
           if(oriAsset == null)
           {
               //create new asset
               if (customAssetCreationMethod != null)
               {
                   Debug.Log("Create new asset");
                   var obj = customAssetCreationMethod.Invoke(prefabPath);
                   var fileName = Path.GetFileName(prefabPath);
                   //remove extension
                   fileName = fileName.Substring(0, fileName.Length - Path.GetExtension(fileName).Length);
                   if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)))
                       AssetDatabase.CreateAsset(obj, prefabFolderPath +"/" + fileName + assetExtension);
                   return obj;
               }
               return null;
           }
           
           var originalPath = AssetDatabase.GetAssetPath(oriAsset);
         
           if(Path.GetDirectoryName(originalPath) == prefabFolderPath)
           {
                Debug.LogError("Same Folder, Move Prefab to another folder");
                return null;
           }

           //extension of asset
           var extension = Path.GetExtension(originalPath);
           var newFilePath = prefabFolderPath + "/" + oriAsset.name+" Copied "+extension;
           Debug.Log("Copy Asset to:" + newFilePath);
           AssetDatabase.CopyAsset(originalPath, newFilePath);
           var newAsset = AssetDatabase.LoadAssetAtPath<T>(newFilePath);
           //生asset沒有得undo唷
           // Undo.RegisterCreatedObjectUndo(newAsset, "Copy Asset");
           return newAsset;
        }
        
        private static void CreateFolderIfNotExist(string folderPath)
        {
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }
        }

        public static T CreateAsset<T>(string folderPath, string fileName) where T : ScriptableObject
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayProgressBar("CreateAsset", fileName, 0.5f);
            CreateFolderIfNotExist(folderPath);


            var data = AssetDatabase.LoadAssetAtPath<T>(folderPath + "/" + fileName + ".asset");
            if (data != null)
            {
                Debug.LogWarning("data already exist");
                EditorUtility.ClearProgressBar();
                return data;
            }
            
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, folderPath + "/" + fileName + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            return asset;
        }
#endif
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
        public static string GetAssetGUID(this Object obj)
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
                    CreateScriptableObject(type,
                            GameStateAttribute.GetFullPath(refObj.gameObject, subFolderName)) as
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
                var fileName = GameStateAttribute.GetFileName(refObj.gameObject) + autoGenGameState.MyGuid +
                               ".asset";
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