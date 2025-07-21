using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

//景要存擋才會有...有沒有別招
[InitializeOnLoad]
public class AutoAttributeManagerEditor : UnityEditor.AssetModificationProcessor
{
    
    // [InitializeOnLoadMethod]
    // static void Init()
    // {
    //     // EditorApplication.playModeStateChanged += PlayModeChanged;
    // }

    // private static void PlayModeChanged(PlayModeStateChange obj)
    // {
    //     if (obj == PlayModeStateChange.ExitingEditMode)
    //     {
    //         AutoAttributeManager.BuildFieldCache();
    //     }
    // }
    private static void MakeSureAutoManagerIsInScene()
    {
        var autoManagers = GameObject.FindObjectsByType<AutoAttributeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool noAutoAttributeManager_inScene = autoManagers == null || autoManagers.Length == 0;
        if (noAutoAttributeManager_inScene)
        {
            InstantiateAutoAttributeManager_InScene();
        }
        else if (autoManagers.Length >= 2)
        {
            autoManagers.Skip(1).ToList().ForEach(DestroyAutoAttributeManager);
        }
        // Debug.Log("Find autoManagers");
    }

    private static void DestroyAutoAttributeManager(AutoAttributeManager autoAttributeManager)
    {
        GameObject.DestroyImmediate(autoAttributeManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void InstantiateAutoAttributeManager_InScene()
    {
        GameObject autoGo = new GameObject("Auto_Attribute_Manager");
        autoGo.AddComponent<AutoAttributeManager>();
        //Make scene dirty, to notify it has changed following the creation of the AutoAttributeManager
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static string[] OnWillSaveAssets(string[] paths)
    {
        MakeSureAutoManagerIsInScene();

        // var autoManager = GameObject.FindObjectOfType<AutoAttributeManager>();
        // autoManager.SweeepScene();

        // autoManager.CacheMonobehavioursWithAuto();
        return paths;
    }
}
