using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core;
using MonoFSM.Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System;
using _1_MonoFSM_Core.Runtime._3_FlagData;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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

            // Listen for prefab stage opened events
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;

            // Listen for prefab stage closed events
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
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

        // public static async UniTask ScanScene(string sceneName, float percent)
        // {
        //     Debug.Log("Scan Scene: " + sceneName);
        //
        //     EditorUtility.DisplayProgressBar("Open Scene", sceneName, 0);
        //     EditorSceneManager.OpenScene(sceneName);
        //     Debug.Log("OpenScene Scene: " + sceneName);
        //     await UniTask.Delay(100);
        //     //how to wait particle system to simulate?
        //     FindSceneSavingAndProcess();
        //     EditorUtility.DisplayProgressBar("Save Scene", sceneName, 0);
        //     Debug.Log("Scan Scene Done: " + sceneName);
        //     EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        //     AssetDatabase.SaveAssets();
        //
        //     Debug.Log("Save Scene Done: " + sceneName);
        //     EditorUtility.ClearProgressBar();
        //     // EditorUtility.ClearProgressBar();
        // }

        [MenuItem("MonoFSM/Reset To PlayTest GameSetting #_R", false, 3)]
        private static void ResetToPlayTest()
        {
            if (Application.isPlaying)
                return;

            Debug.Log("ResetToPlayTest");
            ProcessSceneComponents<IEditorResetToPlayTest>(
                obj => obj.OnEditorResetToPlayTest(),
                progressBarLabel: "Reset To PlayTest"
            );

            // EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            // AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            Debug.Log("ResetToPlayTest Done");
        }

        /// <summary>
        /// 掃描 active scene 中所有實作 T 的 MonoBehaviour，依序呼叫 action。
        /// </summary>
        private static void ProcessSceneComponents<T>(
            Action<T> action,
            bool reverseOrder = false,
            bool setDirty = false,
            string progressBarLabel = null
        ) where T : class
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var allComponents = new List<T>();
            var temp = new List<T>();

            foreach (var gobj in rootGameObjects)
            {
                temp.Clear();
                gobj.GetComponentsInChildren(true, temp);
                allComponents.AddRange(temp);
            }

            if (reverseOrder)
                allComponents.Reverse();

            var total = allComponents.Count;
            for (var i = 0; i < total; i++)
            {
                var component = allComponents[i];
                if (progressBarLabel != null)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            progressBarLabel,
                            $"{typeof(T).Name} {i + 1}/{total}",
                            (float)(i + 1) / total
                        ))
                        return;
                }

                try
                {
                    action(component);
                }
                catch (Exception e)
                {
                    Debug.LogError(e, component as Object);
                }

                if (setDirty && component is Object unityObj)
                    EditorUtility.SetDirty(unityObj);
            }
        }


        private static void OnSceneClosing(Scene scene, bool removingscene)
        {
            //要存？
            //但play的時候不會觸發，只能仰賴手動存
            //Debug Setting設說我不想要管這件事？
            EditorUtility.DisplayDialog(
                "Exit Scene: ValidateBeforeSave",
                "Call OnBefore Scene Save?",
                "ok",
                "cancel"
            );
        }

        [MenuItem("MonoFSM/檢查式存檔 Save Scene with BeforeSave Callback #_S")] //Shift + S
        private static void CustomSave()
        {
            if (Application.isPlaying)
                return;
            // EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                var prefabRoot = prefabStage.prefabContentsRoot;
                // OnPrefabSaving(prefabRoot);
                Debug.Log("On Prefab CustomSave", prefabRoot);
                OnCustomPrefabSaving(prefabRoot);
                return;
            }

            Debug.Log("On Scene CustomSave");
            CustomFindSceneSavingAndProcess();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        private static void OnPrefabSaving(GameObject prefab)
        {
            Debug.Log("OnPrefabSaving");
            var savingObjs = new List<IBeforePrefabSaveCallbackReceiver>();
            prefab.GetComponentsInChildren(true, savingObjs);
            savingObjs.Reverse();
            foreach (var savingObj in savingObjs)
            {
                if (savingObj != null)
                    savingObj.OnBeforePrefabSave();
            }

            // var rootGameObjects = prefab.GetComponentsInChildren<ISceneSavingCallbackReceiver>(true);
            // foreach (var savingObj in rootGameObjects)
            // {
            //     savingObj.OnBeforeSceneSave();
            // }
        }

        private static void OnCustomPrefabSaving(GameObject prefab)
        {
            Debug.Log("OnCustomPrefabSaving");
            var callbackObjs = new List<ICustomPrefabSaveCallbackReceiver>();
            prefab.GetComponentsInChildren(true, callbackObjs);
            callbackObjs.Reverse();
            foreach (var callbackObj in callbackObjs)
            {
                if (callbackObj != null)
                {
                    try
                    {
                        callbackObj.OnCustomPrefabSave();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("OnCustomPrefabSave error", callbackObj as Object);
                        Debug.LogError(e);
                    }
                }
            }
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            Debug.Log("OnPrefabStageOpened: " + prefabStage.assetPath);
            var prefabRoot = prefabStage.prefabContentsRoot;
            var openCallbackObjs = new List<IAfterPrefabStageOpenCallbackReceiver>();
            prefabRoot.GetComponentsInChildren(true, openCallbackObjs);
            AutoAttributeManager.AutoReferenceAllChildren(prefabRoot);
            foreach (var callbackObj in openCallbackObjs)
                if (callbackObj != null)
                    callbackObj.OnAfterPrefabStageOpen();
        }

        private static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            Debug.Log("OnPrefabStageClosing: " + prefabStage.assetPath);
            var prefabRoot = prefabStage.prefabContentsRoot;
            var components = prefabRoot.GetComponentsInChildren<AbstractDescriptionBehaviour>(true);

            foreach (var component in components)
                if (component != null)
                {
                    // Reset prefab stage mode via reflection (since _isPrefabStageMode is private)
                    var field = typeof(AbstractDescriptionBehaviour).GetField(
                        "_isPrefabStageMode",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    if (field != null)
                        field.SetValue(component, false);
                }
        }

        //原本的Save監聽
        private static void OnSceneSaving(Scene scene, string path)
        {
            Debug.Log("OnSceneSaving");

            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootGameObjects)
            {
                //TODO:IOnBuildSceneSavingCallbackReceiver 跟ISceneSavingCallbackReceiver 是不是沒差？
                var receivers = obj.GetComponentsInChildren<IOnBuildSceneSavingCallbackReceiver>(
                    true
                );
                foreach (var r in receivers)
                    try
                    {
                        r.OnBeforeBuildSceneSave();
                    }
                    catch (Exception e) { }

                var receiversold = obj.GetComponentsInChildren<ISceneSavingCallbackReceiver>(true);
                foreach (var r in receiversold)
                    try
                    {
                        r.OnBeforeSceneSave();
                    }
                    catch (Exception e) { }
            }

            var startTime = Time.realtimeSinceStartup;
            var sceneCacheManager = Object.FindObjectOfType<AutoAttributeManager>();
            sceneCacheManager.monoReferenceCache.SaveReferenceCache();
            var endTime = Time.realtimeSinceStartup;
            Debug.Log(
                "OnPostprocessScene:"
                    + SceneManager.GetActiveScene().name
                    + " take:"
                    + (endTime - startTime)
            );
            EditorUtility.ClearProgressBar();
        }

        public static void StoreReferenceCacheOfScene()
        {
            var autoAttributeManager = Object.FindObjectOfType<AutoAttributeManager>();
            if (autoAttributeManager != null)
                autoAttributeManager.monoReferenceCache.SaveReferenceCache();
        }

        public static void FindAllSOAndProcessSceneSave() //ProcessCustomHeavySave?
        {
            // gameFlagDataList.Clear();
            // Debug.Log("Find GameFlag:" + typeof(T).FullName);
            // var myPath = AssetDatabase.GetAssetPath(this);
            // Debug.Log("Mypath" + name + ":" + myPath);
            // var dirPath = System.IO.Path.GetDirectoryName(myPath);
            var filter = "t:" + nameof(AbstractSOConfig);
            Debug.Log("Find All SO with filter:" + filter);
            var allProjectFlags = AssetDatabase.FindAssets(filter);
            // var soList = new List<ScriptableObject>();
            //All 10_Flags
            // string[] allProjectFlags = AssetDatabase.FindAssets("t:GameFlagBase", new[] { "Assets/10_Flags" });
            for (var i = 0; i < allProjectFlags.Length; i++)
            {
                // Debug.Log("Find Flag:" + i + "/" + allProjectFlags.Length);
                var path = AssetDatabase.GUIDToAssetPath(allProjectFlags[i]);
                //這步驟感覺有點貴...只弄一個folder?或是篩選一層類別？
                var flag = AssetDatabase.LoadAssetAtPath<AbstractSOConfig>(path);
                if (flag is ISceneSavingCallbackReceiver sceneSavingCallbackReceiver)
                    sceneSavingCallbackReceiver.OnBeforeSceneSave();
                if (flag is ISceneSavingAfterCallbackReceiver sceneSavingAfterCallbackReceiver)
                    sceneSavingAfterCallbackReceiver.OnAfterSceneSave();
                if (
                    flag is ICustomHeavySceneSavingCallbackReceiver heavySceneSavingCallbackReceiver
                )
                {
                    Debug.Log(
                        "OnHeavySceneSaving called in" + heavySceneSavingCallbackReceiver,
                        heavySceneSavingCallbackReceiver as Object
                    );
                    heavySceneSavingCallbackReceiver.OnHeavySceneSaving();
                }

                // soList.Add(flag);
            }
        }



        private static void CustomFindSceneSavingAndProcess()
        {
            FindAllSOAndProcessSceneSave();

            try
            {
                // Heavy pass（無需 reverse / setDirty）
                ProcessSceneComponents<ICustomHeavySceneSavingCallbackReceiver>(
                    obj => obj.OnHeavySceneSaving(),
                    progressBarLabel: "Scene Saving"
                );

                // Before save pass：倒著叫才會從葉子到根, culling Group才會對
                ProcessSceneComponents<ISceneSavingCallbackReceiver>(
                    obj => obj.OnBeforeSceneSave(),
                    reverseOrder: true,
                    setDirty: true,
                    progressBarLabel: "Scene Saving"
                );

                //會有些物件被重建，所以要重新抓
                StoreReferenceCacheOfScene();

                // After save pass：同樣由下往上
                ProcessSceneComponents<ISceneSavingAfterCallbackReceiver>(
                    obj => obj.OnAfterSceneSave(),
                    reverseOrder: true,
                    setDirty: true,
                    progressBarLabel: "Scene Saving"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.DisplayDialog("Error", e.Message, "ok");
            }

            EditorUtility.ClearProgressBar();
        }

#endif
    }
}
