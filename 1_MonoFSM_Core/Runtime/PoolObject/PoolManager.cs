using System;
using System.Collections.Generic;
using System.Text;
using MonoFSM.AddressableAssets;
using MonoFSM.Runtime;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

public delegate void BeforeActiveHandler(PoolObject obj);

/// <summary>
/// 池管理器 - 管理所有物件池的建立、管理和生命週期
/// </summary>
[DefaultExecutionOrder(10000)] //晚點被destroy, 好像是對的喔
public class PoolManager : MonoBehaviour, IPoolManager
{
    // public static void PreparePoolObjectImplementation(PoolObject obj)
    // {
    // }

    // public static void HandleGameLevelConfigSetting(MonoBehaviour level)
    // {
    //     var ILevelConfigs = new List<ILevelConfig>(level.GetComponentsInChildren<ILevelConfig>(true));
    //
    //     foreach (var item in ILevelConfigs)
    //     {
    //         if (item == null)
    //             continue;
    //         try
    //         {
    //             item.SetLevelConfig();
    //         }
    //         catch (Exception e)
    //         {
    //             if (item is MonoBehaviour)
    //                 Debug.LogError(e.StackTrace, item as MonoBehaviour);
    //             else
    //                 Debug.LogError(e.StackTrace);
    //         }
    //     }
    // }

    /// <summary>
    /// 場景重置和重新載入處理
    /// </summary>
    public static void ResetReload(GameObject root)
    {
        SceneLifecycleManager.ResetReload(root);
    }

    public static void OnBeforeDestroyScene(Scene s)
    {
        SceneLifecycleManager.OnBeforeDestroyScene(s);
    }

    [Obsolete("Use SceneLifecycleManager.LevelResetChildrenReload instead")]
    public static void LevelResetChildrenReload(GameObject gObj)
    {
        SceneLifecycleManager.LevelResetChildrenReload(gObj);
    }

    [Obsolete("Use SceneLifecycleManager.LevelResetStart instead")]
    public static void LevelResetStart(GameObject gObj)
    {
        SceneLifecycleManager.LevelResetStart(gObj);
    }

    [Obsolete("Use SceneLifecycleManager.HandleGameLevelAwake instead")]
    public static void HandleGameLevelAwake(GameObject level)
    {
        SceneLifecycleManager.HandleGameLevelAwake(level);
    }

    // public static void HandleEnterLevelReset(GameObject level)
    // {
    //     var ILevelResets = new List<IResetter>(level.GetComponentsInChildren<IResetter>(true));
    //     // ILevelAwakes.Reverse();
    //     foreach (var item in ILevelResets)
    //     {
    //         if (item == null)
    //             continue;
    //         try
    //         {
    //             item.EnterLevelReset();
    //         }
    //         catch (Exception e)
    //         {
    //             if (item is MonoBehaviour behaviour)
    //                 Debug.LogError(e.StackTrace, behaviour);
    //             else
    //                 Debug.LogError(e.StackTrace);
    //         }
    //     }
    // }


    // [Obsolete("Use SceneLifecycleManager.HandleGameLevelAwakeReverse instead")]
    // public static void HandleGameLevelAwakeReverse(GameObject level)
    // {
    //     SceneLifecycleManager.HandleGameLevelAwakeReverse(level);
    // }
    //
    // [Obsolete("Use SceneLifecycleManager.HandleGameLevelStart instead")]
    // public static void HandleGameLevelStart(GameObject level)
    // {
    //     SceneLifecycleManager.HandleGameLevelStart(level);
    // }
    //
    // [Obsolete("Use SceneLifecycleManager.HandleGameLevelStartReverse instead")]
    // public static void HandleGameLevelStartReverse(GameObject level)
    // {
    //     SceneLifecycleManager.HandleGameLevelStartReverse(level);
    // }

    // public bool IsReady = false;
    [Header("PrewarmData Logger")]
    public Transform poolbjects;
    public PoolPrewarmData prewarmDataLogger;
    public PoolPrewarmData globalPrewarmDataLogger;

    public void RegisterPoolPrewarmData(MonoBehaviour requester, PoolPrewarmData data)
    {
        if (data == null)
            return;

        foreach (var entry in data.objectEntries)
            records.Add(
                new PoolObjectRequestRecords(requester, entry.prefab, entry.DefaultMaximumCount)
            );
    }

    private void ReCalculatePoolObjectEntries()
    {
        foreach (var loadedAsset in this.allLoadedRCGRefereces)
        {
            loadedAsset.Release();
        }

        allLoadedRCGRefereces.Clear();

        //沒有人需要用了。
        for (int i = records.Count - 1; i >= 0; i--)
        {
            if (records[i]._requester == null)
            {
                records[i].Clear();
                records.RemoveAt(i);
            }
        }

        foreach (var e in PoolObjectEntries)
        {
            e.Clear();
        }

        PoolObjectEntries.Clear();

        for (var i = 0; i < records.Count; i++)
            AddEntry(PoolObjectEntries, records[i]._prefab, records[i]._count);
    }

    public void PoolObjectDestroyed(PoolObject poolobj)
    {
        if (PoolDictionary.ContainsKey(poolobj.OriginalPrefab))
            PoolDictionary[poolobj.OriginalPrefab].PoolObjectOnDestroySignal(poolobj);
    }

    private void AddEntry(List<PoolObjectEntry> list, PoolObject poolObject, int count)
    {
        for (var i = 0; i < list.Count; i++)
            if (list[i].prefab == poolObject)
            {
                list[i].DefaultMaximumCount += count;
                return;
            }

        var entry = new PoolObjectEntry();

        entry.prefab = poolObject;
        entry.DefaultMaximumCount = count;

        list.Add(entry);
    }

    public List<PoolObjectRequestRecords> records = new();

    private List<PoolObjectEntry> PoolObjectEntries;

    [Header("Run Time Data")]
    public Dictionary<PoolObject, ObjectPool> PoolDictionary;
    public List<ObjectPool> allPools;

    protected void Awake()
    {
        PoolObjectEntries = new List<PoolObjectEntry>();
        PoolDictionary = new Dictionary<PoolObject, ObjectPool>();
        allPools = new List<ObjectPool>();

        poolbjects = new GameObject("PoolObjects").transform;
        poolbjects.parent = transform;
        poolbjects.localPosition = Vector3.zero;
        poolbjects.gameObject.SetActive(false);

        // Register this instance with the service locator
        // PoolServiceLocator.RegisterPoolManager(this);

        // Subscribe to scene unload event to handle cleanup before scene destruction
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    protected new void OnDestroy()
    {
        // Unsubscribe from scene unload event
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        // Clear services when manager is destroyed
        // PoolServiceLocator.ClearServices();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        OnBeforeDestroyScene(scene);
    }

    public void PrepareGlobalPrewarmData()
    {
        if (globalPrewarmDataLogger == null)
        {
            globalPrewarmDataLogger = PoolBank.FindGlobalPrewarmData();
            //CleanUp 沒用的資料
#if UNITY_EDITOR
            globalPrewarmDataLogger.objectEntries.RemoveAll((a) => a.prefab == null);
            globalPrewarmDataLogger.objectEntries.RemoveAll((a) => !a.prefab.IsGlobalPool);
            EditorUtility.SetDirty(globalPrewarmDataLogger);
#endif

            if (globalPrewarmDataLogger == null)
            {
                PoolLogger.LogError(
                    "No global prewarm data found. Please create one and assign it to the PoolManager.",
                    this);
                return;
            }

            globalPrewarmDataLogger.PrewarmObjects(this, this);
        }
    }

    public void SetPrewarmData(PoolPrewarmData prewarmData, PoolBank bank)
    {
        this.prewarmDataLogger = prewarmData;
        prewarmData.PrewarmObjects(this, bank);
    }

    //
    //private bool _poolCreated = false;

    // public GameObject BorrowOrInstantiate(
    //     GameObject obj,
    //     Vector3 position = default,
    //     Quaternion rotation = default,
    //     Transform parent = null,
    //     Action<PoolObject> handler = null
    // )
    // {
    //     var hasRequest = obj.TryGetComponent(out PoolRequest request);
    //     var hasPoolObj = obj.TryGetComponent<PoolObject>(out var poolObj);
    //
    //     if (hasRequest)
    //     {
    //         return Borrow(
    //             request.PoolObjectRequests.prefab,
    //             position,
    //             rotation,
    //             parent,
    //             handler
    //         ).gameObject;
    //     }
    //     else if (hasPoolObj)
    //     {
    //         return Borrow(poolObj, position, rotation, parent, handler).gameObject;
    //     }
    //     else
    //     {
    //         PoolLogger.LogWarning("Runtime instantiate - object not pooled");
    //         return Instantiate(obj, position, rotation, parent);
    //     }
    // }

    //FIXME: Addressable?
    //     public async UniTask<GameObject> BorrowOrInstantiateRcgAssetReference(
    //         RCGAssetReference obj,
    //         Vector3 position = default,
    //         Quaternion rotation = default,
    //         Transform parent = null,
    //         Action<PoolObject> handler = null
    //     )
    //     {
    //         GameObject poolObject = null;
    //         if (prewarmDataLogger != null)
    //         {
    //             poolObject = prewarmDataLogger.TryFindPrefab(obj.AssetReference);
    //
    //             if (poolObject != null)
    //                 return BorrowOrInstantiate(poolObject, position, rotation, parent, handler);
    //         }
    //
    //         PoolLogger.LogWarning($"Please prewarm asset: {obj.AssetReference.AssetGUID}");
    // #if UNITY_EDITOR
    //         if (obj.editorAsset != null)
    //             PoolLogger.LogWarning("Please prewarm this asset", obj.editorAsset);
    // #endif
    //
    //         if (allLoadedRCGRefereces.Contains(obj) == false)
    //         {
    //             allLoadedRCGRefereces.Add(obj);
    //         }
    //
    //         poolObject = await obj.GetAssetAsync<GameObject>();
    //
    //         if (this.prewarmDataLogger != null)
    //             this.prewarmDataLogger.RegisterEntry(poolObject, obj.AssetReference);
    //
    //         return BorrowOrInstantiate(poolObject, position, rotation, parent, handler);
    //
    //         return null;
    //     }

    public List<RCGAssetReference> allLoadedRCGRefereces = new List<RCGAssetReference>();

    public T BorrowOrInstantiate<T>(T obj, Transform parent)
        where T : MonoBehaviour
    {
        return BorrowOrInstantiate(obj, Vector3.zero, Quaternion.identity, parent);
    }

    //FIXME: 進入點不開裸露？
    public T BorrowOrInstantiate<T>(
        T obj,
        Vector3 position = default,
        Quaternion rotation = default,
        Transform parent = null,
        Action<PoolObject> handler = null
    )
        where T : MonoBehaviour
    {
        if (obj == null)
        {
            PoolLogger.LogError("BorrowOrInstantiate: obj is null");
            return null;
        }

        if (obj is PoolObject pObj)
            // 如果是 PoolObject，直接使用 Borrow 方法
            return Borrow(pObj, position, rotation, parent, handler).GetComponent<T>();

        if (obj.TryGetComponent<PoolObject>(out var poolObj))
        {
            return Borrow(poolObj, position, rotation, parent, handler).GetComponent<T>();
        }

        //error handling而已，直接拔掉好了？
        PoolLogger.LogError("Prefab is not a pool object, using Instantiate", obj);

        //             var result = Instantiate(obj, position, rotation, parent);
        // #if UNITY_EDITOR
        //             //給spawner做？PoolObject第一次拿出來才需要？
        //             //Editor裡還是直接使用AutoAttributeManager，cache太容易髒掉了
        //             Debug.Log($"AutoReferenceAllChildren: {result.name}", result);
        //             AutoAttributeManager.AutoReferenceAllChildren(result.gameObject);
        // #endif
        return null;
    }

    private PoolObject Borrow(
        PoolObject prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null,
        Action<PoolObject> handler = null
    )
    {
        // Prevent borrowing during pool recalculation to avoid inconsistent state
        if (_recalculating)
        {
            PoolLogger.LogError($"Cannot borrow during recalculation: {prefab.name}", prefab);
            return null;
        }

        //FIXME: 借用場上的，編輯參數蠻好用？看要不要砍掉？
        if (prefab.UseSceneAsPool)
        {
            // 初始重置
            //FIXME: 太深了？不好理解
            prefab.TransformReset();
            prefab.PoolObjectResetAndStart();

            // 設置新的 Transform 位置
            var defaultScale = TransformResetHelper.GetDefaultScale(prefab); //scale清乾淨 (回到Prefab的scale)
            // prefab.OverrideTransformSetting(position, rotation, parent, defaultScale);
            // prefab.TransformReset();

            prefab.OnBorrowFromPool(null);
            prefab.gameObject.SetActive(true);
            prefab.PoolObjectResetAndStart();

            handler?.Invoke(prefab);

            return prefab;
        }
        // prefab

        if (!PoolDictionary.ContainsKey(prefab))
        {
            // Pool not found, create new pool on demand
            AddAPool(prefab);
            PoolDictionary[prefab].UpdatePoolEntry();
        }

        return PoolDictionary[prefab].Borrow(position, rotation, parent, handler);
    }

    //從poolProvider的進入點
    public T BorrowOrInstantiate<T>(
        GameObject obj,
        Vector3 position = default,
        Quaternion rotation = default,
        Transform parent = null,
        Action<PoolObject> handler = null
    )
    {
        throw new NotImplementedException();
    }

    public void ReturnToPool(PoolObject instance)
    {
        PoolDictionary[instance.OriginalPrefab].ReturnToPool(instance);
    }

    /// <summary>
    /// Return MonoPoolObj to pool (not yet implemented)
    /// </summary>
    public void ReturnToPool(MonoObj obj)
    {
        if (obj.TryGetComponent<PoolObject>(out var poolObj))
        {
            if (poolObj.IsFromPool)
            {
                ReturnToPool(poolObj);
                return;
            }
        }

        //fallback: if it's not a PoolObject, just deactivate it
        obj.WorldUpdateSimulator.UnregisterMonoObject(obj);
        obj.gameObject.SetActive(false); // destroy?
        Debug.LogError(
            $"Deactive object {obj.name} because it has no PoolObject component.");
    }

    //

    private bool _recalculating = false;

    /// <summary>
    /// 重新計算所有池的大小和狀態
    /// </summary>
    public void ReCalculatePools()
    {
        _recalculating = true;

        Profiler.BeginSample("ReCalculatePools");

        //把null game level 清掉了
        ReCalculatePoolObjectEntries();

        Profiler.BeginSample("DestroyNonUsedPool");
        for (var i = allPools.Count - 1; i >= 0; i--)
        {
            var currentPool = allPools[i];
            //檢查是否應該保持池存活（考慮明確請求和受保護物件）
            var entry = ShouldKeepPoolAlive(currentPool._prefab);

            //移除沒用到的pool
            if (entry == null)
            {
                // 額外的安全檢查：確保沒有受保護物件被意外銷毀
                if (currentPool.HasProtectedObjects())
                {
                    var protectedCount = currentPool.GetProtectedObjectCount();
                    PoolLogger.LogError(
                        $"Attempted to destroy pool with {protectedCount} protected objects: {currentPool._prefab.name}. Destruction prevented."
                    );

                    // 創建緊急 Entry 以保護這些物件
                    var emergencyEntry = new PoolObjectEntry
                    {
                        prefab = currentPool._prefab,
                        DefaultMaximumCount = protectedCount,
                    };
                    allPools[i]._bindingEntry = emergencyEntry;
                    continue;
                }

                PoolLogger.LogInfo($"Destroying unused pool: {currentPool._prefab.name}");
                PoolDictionary.Remove(currentPool._prefab);
                allPools[i].DestroyPool();
                allPools[i] = null;
                allPools.RemoveAt(i);
            }
            //綁定新的Entry
            else
            {
                allPools[i]._bindingEntry = entry;
            }
        }

        Profiler.EndSample();

        Profiler.BeginSample("ScalePoolToNewMaximum");
        for (var i = 0; i < allPools.Count; i++)
            allPools[i].ScalePoolToNewMaximum();
        Profiler.EndSample();

        Profiler.BeginSample("Add New Entry");
        //增加新的沒看過的Entry
        for (var i = 0; i < PoolObjectEntries.Count; i++)
        {
            if (PoolDictionary.ContainsKey(PoolObjectEntries[i].prefab) == false)
            {
                var pool = new ObjectPool(PoolObjectEntries[i], this);
                allPools.Add(pool);
                pool.Init();
                PoolDictionary.Add(PoolObjectEntries[i].prefab, pool);
            }
        }

        Profiler.EndSample();

        // sw.Stop();
        // Debug.Log("[PoolManager] Prepare ElapsedMilliseconds:" + sw.ElapsedMilliseconds);
        // UnityEngine.Debug.LogFormat("[Auto] Assigned <color={5}><b>{4}/{2}</b></color> [Auto*] variables in <color=#cc3300><b>{3} Milliseconds </b></color> - Analized {0} MonoBehaviours and {1} variables",
        //    monoBehavioursInSceneWithAuto.Count(), variablesAnalized, variablesWithAuto, sw.ElapsedMilliseconds, autoVarialbesAssigned_count, autoVarialbesAssigned_count + autoVarialbesNotAssigned_count, result_color);
        Profiler.EndSample();

        _recalculating = false;
    }

    public void ReturnAllObjects()
    {
        // Debug.Log("Return All PoolObj");
        for (var i = 0; i < allPools.Count; i++)
            allPools[i].ReturnAllObjects();
    }

    public void ReturnAllObjects(Scene withScene)
    {
        // Debug.Log("Return All PoolObj With Scene");
        for (var i = 0; i < allPools.Count; i++)
            allPools[i].ReturnAllObjects(withScene);
    }

    public delegate bool PoolPredicate(PoolObject p);

    public void ReturnAllObjects(Scene withScene, PoolPredicate poolPredicate)
    {
        var StillOnUses = new List<PoolObject>();

        for (var i = 0; i < allPools.Count; i++)
        {
            if (poolPredicate(allPools[i]._prefab))
            {
                allPools[i].ReturnAllObjects(withScene);
            }
        }
    }

    private void AddAPool(PoolObject obj)
    {
        if (PoolDictionary.ContainsKey(obj))
            return;

        var entry = new PoolObjectEntry();
        entry.prefab = obj;
        entry.DefaultMaximumCount = 1;

        var pool = new ObjectPool(entry, this);

        allPools.Add(pool);
        pool.Init();

        PoolDictionary.Add(obj, pool);
    }

    /// <summary>
    /// 檢查是否應該保持池存活
    /// 考慮兩個因素：1) 明確的請求 2) 受保護的物件
    /// </summary>
    public PoolObjectEntry ShouldKeepPoolAlive(PoolObject prefab)
    {
        // 首先檢查是否有明確的請求
        for (var i = PoolObjectEntries.Count - 1; i >= 0; i--)
            if (PoolObjectEntries[i].prefab == prefab)
                return PoolObjectEntries[i];

        // 如果沒有明確請求，檢查是否有受保護的物件
        if (PoolDictionary.ContainsKey(prefab))
        {
            var pool = PoolDictionary[prefab];
            if (pool.HasProtectedObjects())
            {
                int protectedCount = pool.GetProtectedObjectCount();
                PoolLogger.LogInfo(
                    $"Keeping pool {prefab.name} alive due to {protectedCount} protected objects"
                );

                // 為受保護物件創建一個虛擬的 Entry
                return new PoolObjectEntry
                {
                    prefab = prefab,
                    DefaultMaximumCount = Mathf.Max(
                        1,
                        protectedCount
                    ) // 至少保持足夠容納受保護物件的大小
                    ,
                };
            }
        }

        return null;
    }

    /// <summary>
    /// 舊方法名稱的向後兼容性（已過時）
    /// </summary>
    [Obsolete("Use ShouldKeepPoolAlive instead")]
    public PoolObjectEntry isInRequest(PoolObject prefab)
    {
        return ShouldKeepPoolAlive(prefab);
    }

    /// <summary>
    /// 獲取整個池系統的受保護物件狀態報告
    /// </summary>
    public string GetSystemProtectedObjectsReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== Pool System Protected Objects Report ===");

        int totalProtected = 0;
        int poolsWithProtected = 0;

        foreach (var pool in allPools)
        {
            if (pool != null && pool.HasProtectedObjects())
            {
                poolsWithProtected++;
                int protectedCount = pool.GetProtectedObjectCount();
                totalProtected += protectedCount;

                report.AppendLine(
                    $"Pool '{pool._prefab.name}': {protectedCount} protected objects"
                );
            }
        }

        report.AppendLine(
            $"Summary: {totalProtected} protected objects across {poolsWithProtected} pools (total pools: {allPools.Count})"
        );
        return report.ToString();
    }

    /// <summary>
    /// 驗證整個池系統的完整性
    /// </summary>
    public bool ValidateSystemIntegrity()
    {
        bool allValid = true;

        PoolLogger.LogInfo("Starting system integrity validation...");

        foreach (var pool in allPools)
        {
            if (pool != null)
            {
                // Basic validation: check if pool has protected objects and report
                if (pool.HasProtectedObjects())
                {
                    int protectedCount = pool.GetProtectedObjectCount();
                    PoolLogger.LogInfo(
                        $"Pool {pool._prefab.name} has {protectedCount} protected objects"
                    );
                }

                // Validate basic object counts
                int totalObjects = pool.AllObjs?.Count ?? 0;
                int inUseObjects = pool.OnUseObjs?.Count ?? 0;
                int availableObjects = pool.DisabledObjs?.Count ?? 0;

                if (totalObjects != inUseObjects + availableObjects)
                {
                    PoolLogger.LogError(
                        $"Pool {pool._prefab.name} has inconsistent object counts: Total={totalObjects}, InUse={inUseObjects}, Available={availableObjects}"
                    );
                    allValid = false;
                }
            }
        }

        if (allValid)
        {
            PoolLogger.LogInfo("System integrity validation passed");
        }
        else
        {
            PoolLogger.LogError(
                "System integrity validation failed - check individual pool errors above"
            );
        }

        return allValid;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to log detailed pool status
    /// </summary>
    [MenuItem("Tools/Pool System/Log Protected Objects Report")]
    public static void LogProtectedObjectsReport()
    {
        // if (Instance != null)
        // {
        //     Debug.Log(Instance.GetSystemProtectedObjectsReport());
        // }
        // else
        // {
        //     Debug.LogWarning("PoolManager instance not found");
        // }
    }

    [MenuItem("Tools/Pool System/Validate System Integrity")]
    public static void ValidateSystem()
    {
        // if (Instance != null)
        // {
        //     Instance.ValidateSystemIntegrity();
        // }
        // else
        // {
        //     Debug.LogWarning("PoolManager instance not found");
        // }
    }
#endif
}
