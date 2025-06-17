using MonoFSM.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MonoFSM.Runtime
{
    public static class PoolObjectUtility
    {
        public static void OnPrefabSaving(GameObject prefab)
        {
#if UNITY_EDITOR
            Debug.Log("OnPrefabSaving");
            var savingObjs = prefab.GetComponentsInChildren<IBeforePrefabSaveCallbackReceiver>(true);
            foreach (var savingObj in savingObjs)
            {
                savingObj.OnBeforePrefabSave();
                UnityEditor.EditorUtility.SetDirty(savingObj as MonoBehaviour);
            }
            AutoAttributeManager.AutoReferenceAllChildren(prefab);
            // var rootGameObjects = prefab.GetComponentsInChildren<ISceneSavingCallbackReceiver>(true);
            // foreach (var savingObj in rootGameObjects)
            // {
            //     savingObj.OnBeforeSceneSave();
            // }
#endif
        }

        public static void GenCacheForAllPrefabs(PoolPrewarmData prewarmData)
        {
#if UNITY_EDITOR
            //open all prefabs in prefab stage and save

            foreach (var entry in prewarmData.objectEntries)
            {
                var path = AssetDatabase.GetAssetPath(entry.prefab);
                // var prefabStage = PrefabStageUtility.OpenPrefab(path);

                var prefabContents = PrefabUtility.LoadPrefabContents(path);

                // Modify the prefab (example: change the name of the root object)
                OnPrefabSaving(prefabContents);
                // Save the modified contents back to the prefab
                PrefabUtility.SaveAsPrefabAsset(prefabContents, path);

                // Unload the prefab contents
                PrefabUtility.UnloadPrefabContents(prefabContents);

                //save the prefab
                // EditorSceneManager.SaveScene(prefabStage.scene);
            }
#endif
        }
    }
}