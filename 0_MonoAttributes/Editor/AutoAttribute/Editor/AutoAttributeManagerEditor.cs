using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//景要存擋才會有...有沒有別招
[InitializeOnLoad]
public class AutoAttributeManagerEditor : UnityEditor.AssetModificationProcessor
{
    // 快取沒有 Auto 欄位的型別，避免重複反射
    private static readonly HashSet<Type> _typesWithoutAutoFields = new();
    private static bool _resolveScheduled;

    static AutoAttributeManagerEditor()
    {
        // EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        // Domain reload 後延遲解析一次
        // ScheduleResolve();
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    private static void PlayModeChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.EnteredEditMode)
        {
            // 回到 Editor
            // ScheduleResolve();
        }
    }

    // private static void OnHierarchyChanged()
    // {
    //     if (Application.isPlaying) return;
    //     ScheduleResolve();
    // }

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        if (Application.isPlaying) return;
        // 場景切換時清除快取，因為型別可能不同
        _typesWithoutAutoFields.Clear();
        ScheduleResolve();
    }

    /// <summary>
    /// 排程一次延遲解析（debounce：多次 hierarchyChanged 只跑一次）
    /// </summary>
    private static void ScheduleResolve()
    {
        if (_resolveScheduled) return;
        _resolveScheduled = true;
        EditorApplication.delayCall += ResolveAllAutoFieldsInScene;
    }

    /// <summary>
    /// 解析場景中所有帶 [Auto*] 欄位的 MonoBehaviour。
    /// 利用 FieldCache 的 type-level 快取：已知沒有 Auto 欄位的型別直接跳過，
    /// 只有首次碰到新型別時才需要反射，後續幾乎零成本。
    /// </summary>
    private static void ResolveAllAutoFieldsInScene()
    {
        _resolveScheduled = false;
        if (Application.isPlaying) return;

        // 若場景中的 AutoAttributeManager 勾了 _useCacheOnly，跳過這次 sweep
        var managers = UnityEngine.Object.FindObjectsByType<AutoAttributeManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        if (managers != null && managers.Length > 0 &&
            managers.Any(m => m != null && m.UseCacheOnly))
        {
            return;
        }

        var mbs = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var mb in mbs)
        {
            if (mb == null) continue;

            var type = mb.GetType();

            // 快速跳過已知沒有 Auto 欄位的型別
            if (_typesWithoutAutoFields.Contains(type)) continue;

            // 檢查此型別是否有 Auto 欄位（透過 FieldCache 快取）
            var fields = AutoAttributeManager.GetFieldsWithAutoAndBuildCache(mb);
            if (fields == null || !fields.Any())
            {
                _typesWithoutAutoFields.Add(type);
                continue;
            }

            // 執行 Auto 解析
            AutoAttributeManager.AutoReference(mb);
        }

        Debug.Log(
            $"AutoAttributeManager: Resolved Auto fields in scene. Total MonoBehaviours: {mbs.Length}, Types without Auto fields cached: {_typesWithoutAutoFields.Count}");
    }

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
        var autoManagers = GameObject.FindObjectsByType<AutoAttributeManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        bool noAutoAttributeManager_inScene = autoManagers == null || autoManagers.Length == 0;
        if (noAutoAttributeManager_inScene)
        {
            InstantiateAutoAttributeManager_InScene();
        }
        else if (autoManagers.Length >= 2)
        {
            autoManagers.Skip(1).ToList().ForEach(DestroyAutoAttributeManager);
        }

        Debug.Log("Find autoManagers");
    }

    private static void DestroyAutoAttributeManager(AutoAttributeManager autoAttributeManager)
    {
        Debug.Log("DestroyAutoAttributeManager", autoAttributeManager.gameObject);
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
