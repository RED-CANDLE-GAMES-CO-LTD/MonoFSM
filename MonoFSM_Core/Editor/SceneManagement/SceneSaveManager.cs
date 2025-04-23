#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using RCGMaker.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


//實作ISceneSaving, 就可以在存檔前，把資料寫出去之類的(AutoGen When Save)
namespace EditorTool
{
    public static class SceneSaveManager
    {
        //public static bool IsBuilding = false;
#if UNITY_EDITOR


        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            // EditorSceneManager.sceneClosing -= OnSceneClosing;
            // EditorSceneManager.sceneClosing += OnSceneClosing;
            // EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            //TODO: PrefabStage Save??
            //FIXME: Shift Save? 沒有dirty就不會跑這個喔
            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        // public static async UniTask ScanSceneAndBuildCache(RCGBuildConfig config, bool isTinyBuild = false)
        // {
        //     var i = 0;
        //     foreach (var sceneSetting in config.BuildScenes)
        //     {
        //         if (isTinyBuild && sceneSetting.IncludeInTinyBuild == false)
        //             continue;
        //         i++;
        //         await ScanScene(sceneSetting.SceneName, (float)i / config.BuildScenes.Count);
        //     }
        //
        //     EditorUtility.ClearProgressBar();
        //     AssetDatabase.SaveAssets();
        // }
        //
        // public static async UniTask ScanSceneOfAreaInBuildConfig(RCGBuildConfig config, string areaName)
        // {
        //     var validScenes = config.BuildScenes.FindAll(sceneSetting => FilterArea(areaName, sceneSetting.SceneName));
        //     Debug.Log("Valid Scenes: " + areaName + " ,Count:" + validScenes.Count);
        //     var i = 0;
        //     foreach (var sceneSetting in validScenes)
        //     {
        //         i++;
        //         await ScanScene(sceneSetting.SceneName, (float)i / validScenes.Count);
        //     }
        //
        //     EditorUtility.ClearProgressBar();
        //     AssetDatabase.SaveAssets();
        // }


        private static bool FilterArea(string areaName, string sceneName)
        {
            //scenename包含areaName
            if (!sceneName.Contains(areaName))
                return false;

            return true;
        }

        public static async UniTask ScanScene(string sceneName, float percent)
        {
            Debug.Log("Scan Scene: " + sceneName);

            EditorUtility.DisplayProgressBar("Open Scene", sceneName, 0);
            EditorSceneManager.OpenScene(sceneName);
            Debug.Log("OpenScene Scene: " + sceneName);
            await UniTask.Delay(100);
            //how to wait particle system to simulate?
            FindSceneSavingAndProcess();
            EditorUtility.DisplayProgressBar("Save Scene", sceneName, 0);
            Debug.Log("Scan Scene Done: " + sceneName);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("Save Scene Done: " + sceneName);
            EditorUtility.ClearProgressBar();
            // EditorUtility.ClearProgressBar();
        }

        private static void OnSceneClosing(Scene scene, bool removingscene)
        {
            //要存？
            //但play的時候不會觸發，只能仰賴手動存
            //Debug Setting設說我不想要管這件事？
            EditorUtility.DisplayDialog("Exit Scene: ValidateBeforeSave", "Call OnBefore Scene Save?", "ok", "cancel");
        }

        [MenuItem("RCGs/檢查式存檔 Save Scene with BeforeSave Callback #_S")] //Shift + S
        private static void CustomSave()
        {
            if (Application.isPlaying)
                return;
            // EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            Debug.Log("OnSceneSaving");

            FindSceneSavingAndProcess();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        // [MenuItem("RCGs/檢查prewarm的Prefab有綁到Reference #_P")] //Shift + P
        // private static void AutoBindForAllPrefabs()
        // {
        //     if (Application.isPlaying)
        //         return;
        //     // var prewarmData = Object.FindObjectOfType<PoolPrewarmData>();
        //     // if (prewarmData != null)
        //     // {
        //     //     PoolObjectUtility.AutoBindForAllPrefabs(prewarmData);
        //     // }
        // }

        private static void OnPrefabSaving(GameObject prefab)
        {
            Debug.Log("OnPrefabSaving");
            var savingObjs = new List<IBeforePrefabSaveCallbackReceiver>();
            prefab.GetComponentsInChildren(true, savingObjs);
            savingObjs.Reverse();
            foreach (var savingObj in savingObjs) savingObj.OnBeforePrefabSave();

            // var rootGameObjects = prefab.GetComponentsInChildren<ISceneSavingCallbackReceiver>(true);
            // foreach (var savingObj in rootGameObjects)
            // {
            //     savingObj.OnBeforeSceneSave();
            // }
        }


        private static void OnSceneSaving(Scene scene, string path)
        {
            Debug.Log("OnSceneSaving");

            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootGameObjects)
            {
                var receivers =
                    obj.GetComponentsInChildren<IOnBuildSceneSavingCallbackReceiver>(true);
                foreach (var r in receivers)
                    try
                    {
                        r.OnBeforeBuildSceneSave();
                    }
                    catch (Exception e)
                    {
                    }
            }


            var startTime = Time.realtimeSinceStartup;
            var sceneCacheManager = Object.FindObjectOfType<AutoAttributeManager>();
            sceneCacheManager.monoReferenceCache.SaveReferenceCache();
            var endTime = Time.realtimeSinceStartup;
            Debug.Log("OnPostprocessScene:" + SceneManager.GetActiveScene().name + " take:" +
                      (endTime - startTime));
            EditorUtility.ClearProgressBar();
        }

        public static void StoreReferenceCacheOfScene()
        {
            var autoAttributeManager = Object.FindObjectOfType<AutoAttributeManager>();
            if (autoAttributeManager != null) autoAttributeManager.monoReferenceCache.SaveReferenceCache();
        }

        private static void FindSceneSavingAndProcess()
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            // EditorUtility.ClearProgressBar();
            // StoreReferenceCacheOfScene();
            try
            {
                foreach (var gobj in rootGameObjects)
                {
                    var savingObjs = new List<ISceneSavingCallbackReceiver>();

                    gobj.GetComponentsInChildren(true, savingObjs);
                    savingObjs.Reverse(); //倒著叫才會從葉子到根, culling Group才會對
                    // var savingObjs = gobj.GetComponentsInChildren<ISceneSavingCallbackReceiver>(true);

                    // EditorUtility.DisplayProgressBar("Before Scene Saving", "Processing", 0);
                    var i = 0;
                    var total = savingObjs.Count;

                    foreach (var savingObj in savingObjs)
                    {
                        i++;
                        if (EditorUtility.DisplayCancelableProgressBar("Scene Saving", "OnBeforeSceneSave" + i,
                                (float)i / total))
                            return;

                        try
                        {
                            savingObj.OnBeforeSceneSave();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e, savingObj as MonoBehaviour);
                        }

                        EditorUtility.SetDirty(savingObj as MonoBehaviour);
                    }
                }

                //會有些物件被重建，所以要重新抓
                StoreReferenceCacheOfScene();
                var afterSavingObjs = new List<ISceneSavingAfterCallbackReceiver>();
                foreach (var gobj in rootGameObjects)
                {
                    var i = 0;

                    gobj.GetComponentsInChildren(true, afterSavingObjs);
                    //由下往上跑
                    afterSavingObjs.Reverse();
                    var total = afterSavingObjs.Count;
                    foreach (var afterSaving in afterSavingObjs)
                    {
                        i++;
                        if (EditorUtility.DisplayCancelableProgressBar("Scene Saving", "OnAfterSceneSave" + i,
                                (float)i / total))
                            return;

                        try
                        {
                            afterSaving.OnAfterSceneSave();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("afterSaving error" + afterSaving);
                            Debug.LogError(e, afterSaving as MonoBehaviour);
                        }

                        EditorUtility.SetDirty(afterSaving as MonoBehaviour);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                //show panel
                EditorUtility.DisplayDialog("Error", e.Message, "ok");
            }


            EditorUtility.ClearProgressBar();
        }

#endif
    }
}