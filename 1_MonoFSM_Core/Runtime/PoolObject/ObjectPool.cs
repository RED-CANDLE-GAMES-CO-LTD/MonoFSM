using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// 物件池管理單個類型的 PoolObject 實例
/// 負責物件的創建、借用、回收和智能管理
/// </summary>
public class ObjectPool : IObjectPool
{
    public ObjectPool(PoolObjectEntry bindingEntry, PoolManager manager)
    {
        _bindingEntry = bindingEntry;
        ObjectCount = bindingEntry.DefaultMaximumCount;
        _prefab = bindingEntry.prefab;
        // Debug.Log($"Create ObjectPool for: manager:{manager}");
        _poolManager = manager;
    }

    public PoolObjectEntry _bindingEntry;

    public PoolManager _poolManager;
    public int ObjectCount;

    public List<PoolObject> AllObjs;
    public HashSet<PoolObject> OnUseObjs;
    public List<PoolObject> DisabledObjs;

    public PoolObject _prefab;

    private bool init = false;

    // IObjectPool interface properties
    public int TotalObjectCount => AllObjs?.Count ?? 0;
    public int InUseObjectCount => OnUseObjs?.Count ?? 0;
    public int AvailableObjectCount => DisabledObjs?.Count ?? 0;

    public void PoolObjectOnDestroySignal(PoolObject p)
    {
        if (AllObjs.Contains(p))
        {
            AllObjs.Remove(p);
        }

        if (DisabledObjs.Contains(p))
            DisabledObjs.Remove(p);

        if (OnUseObjs.Contains(p))
            OnUseObjs.Remove(p);
    }

    //現在只有Manual Reset時走這裡
    public void ReturnAllObjects()
    {
        var StillOnUses = new List<PoolObject>();
        StillOnUses.AddRange(OnUseObjs);

        for (var i = 0; i < StillOnUses.Count; i++)
        {
            var obj = StillOnUses[i];
            if (obj._preventResetRecycle)
                continue;
            StillOnUses[i].Recycle();
        }

    }

    public void ReturnAllObjects(Scene scene)
    {
        var stillOnUses = new List<PoolObject>();
        stillOnUses.AddRange(OnUseObjs);
        stillOnUses = stillOnUses.FindAll((a) => a.gameObject.scene == scene);

        // 只回收可以安全回收的物件（不在使用中且未受保護）
        for (var i = 0; i < stillOnUses.Count; i++)
        {
            var obj = stillOnUses[i];
            if (!obj.IsProtected())
            {
                obj.Recycle();
            }
        }
    }

    public void DestroyPool()
    {
        foreach (var obj in AllObjs)
            if (obj && obj.gameObject)
                MonoBehaviour.Destroy(obj.gameObject);
            else
                PoolLogger.LogWarning(
                    $"Pool object {_prefab.gameObject.name} was unexpectedly destroyed"
                );

        AllObjs.Clear();
        OnUseObjs.Clear();
        DisabledObjs.Clear();

        // Debug.Log("DestroyPool:"+_prefab);

        _bindingEntry = null;
        ObjectCount = 0;
        _prefab = null;
        _poolManager = null;
    }

    /*public void SetIsHandledPoolRequestPoolObject(PoolObject p, bool active)
    {
        PoolRequest[] poolRequests = p.GetComponentsInChildren<PoolRequest>(true);

        for (int i = 0; i < poolRequests.Length; i++)
        {
            poolRequests[i].isHandledRequestByPoolManager = active;
        }
    }*/

    public void ScalePoolToNewMaximum()
    {
        AllObjs.RemoveAllNull();
        DisabledObjs.RemoveAllNull();

        // 智能回收：只回收可以安全回收的物件
        SmartReturnObjects();

        if (AllObjs.Count == _bindingEntry.DefaultMaximumCount)
        {
#if RCG_DEV
            // Debug.Log(_bindingEntry.prefab+":AllObjs.Count == _bindingEntry.DefaultMaximumCount:"+_bindingEntry.DefaultMaximumCount);
#endif
            return;
        }
        else if (AllObjs.Count < _bindingEntry.DefaultMaximumCount)
        {
            var offset = _bindingEntry.DefaultMaximumCount - AllObjs.Count;
#if RCG_DEV
            // Debug.Log(_bindingEntry.prefab+":AllObjs.Count < _bindingEntry.DefaultMaximumCount:"+_bindingEntry.DefaultMaximumCount);
#endif
            for (var i = 0; i < offset; i++)
                AddAObject();
        }
        else if (AllObjs.Count > _bindingEntry.DefaultMaximumCount)
        {
            // 漸進式縮減：優先移除可安全回收的物件
            var offset = AllObjs.Count - _bindingEntry.DefaultMaximumCount;
            var removableObjects = new List<PoolObject>();

            // 找出可以移除的物件（從 DisabledObjs 和可安全回收的物件中選擇，但不包含 Protected 物件）
            foreach (var obj in AllObjs)
            {
                if (obj != null && !obj.IsProtected())
                {
                    removableObjects.Add(obj);
                    if (removableObjects.Count >= offset)
                        break;
                }
            }

            PoolLogger.LogDev(
                $"{_bindingEntry.prefab.name}: 嘗試移除 {removableObjects.Count}/{offset} 個物件"
            );

            foreach (var obj in removableObjects)
            {
                if (AllObjs.Contains(obj))
                {
                    AllObjs.Remove(obj);
                    if (DisabledObjs.Contains(obj))
                        DisabledObjs.Remove(obj);
                    if (OnUseObjs.Contains(obj))
                        OnUseObjs.Remove(obj);
                    MonoBehaviour.Destroy(obj.gameObject);
                }
            }
        }

        // 重建 DisabledObjs 列表：只包含未使用且未受保護的物件
        DisabledObjs.Clear();
        foreach (var obj in AllObjs)
        {
            if (obj != null && !OnUseObjs.Contains(obj) && !obj.IsProtected())
            {
                DisabledObjs.Add(obj);
            }
        }
    }

    /// <summary>
    /// 智能回收物件，只回收可以安全回收的物件，保護 Protected 物件
    /// </summary>
    private void SmartReturnObjects()
    {
        var returnableObjects = new List<PoolObject>();

        // 找出所有可以安全回收的使用中物件（未受保護的物件）
        foreach (var obj in OnUseObjs)
        {
            if (obj != null && !obj.IsProtected())
            {
                returnableObjects.Add(obj);
            }
        }

        // 執行回收
        foreach (var obj in returnableObjects)
        {
            obj.Recycle();
        }

        // 記錄保護狀況
        var protectedCount = OnUseObjs.Count(obj => obj != null && obj.IsProtected());
        if (protectedCount > 0)
        {
            PoolLogger.LogPoolRecycle(
                _prefab.name,
                returnableObjects.Count,
                protectedCount,
                _prefab
            );
        }
    }

    /// <summary>
    /// 檢查此池是否包含任何受保護的物件
    /// </summary>
    public bool HasProtectedObjects()
    {
        // 檢查使用中的物件
        foreach (var obj in OnUseObjs)
        {
            if (obj != null && obj.IsProtected())
                return true;
        }

        // 檢查未使用的物件（理論上不應該有 Protected 的未使用物件，但為了安全起見）
        foreach (var obj in DisabledObjs)
        {
            if (obj != null && obj.IsProtected())
                return true;
        }

        return false;
    }

    /// <summary>
    /// 獲取受保護物件的數量
    /// </summary>
    public int GetProtectedObjectCount()
    {
        int count = 0;

        foreach (var obj in OnUseObjs)
        {
            if (obj != null && obj.IsProtected())
                count++;
        }

        foreach (var obj in DisabledObjs)
        {
            if (obj != null && obj.IsProtected())
                count++;
        }

        return count;
    }

    public PoolObject Borrow(
        Vector3 position,
        Quaternion rotation,
        Transform parent = null,
        Action<PoolObject> beforeHandler = null
    )
    {
        //先加新的到pool裡
        if (DisabledObjs.Count == 0)
        {
            // Pool is empty, create a new object
            AddAObject(true);
        }

        if (DisabledObjs.Count > 0)
        {
            var obj = DisabledObjs[0];
            DisabledObjs.RemoveAt(0);
            OnUseObjs.Add(obj);

            obj.OnBorrowFromPool(_poolManager);

            // 設置 Transform 位置和重置狀態
            var resetData = TransformResetHelper.SetupPoolObjectTransform(
                obj,
                position,
                rotation,
                parent
            );
            // obj.OverrideTransformSetting(position, rotation, parent, TransformResetHelper.GetDefaultScale(obj));
            // obj.TransformReset();

            //scale清乾淨 (回到Prefab的scale)
            beforeHandler?.Invoke(obj);
            obj.gameObject.SetActive(true);

            //這裡才是真的onBorrow
            obj.PoolObjectResetAndStart();
            return obj;
        }
        else
        {
            PoolLogger.LogError($"Pool bankrupt: {_prefab.gameObject.name}", _prefab);
            return null;
        }
    }

    public void AddAObject(bool updatePrewarm = false)
    {
        if (_poolManager == null)
            PoolLogger.LogError("PoolManager is null during AddAObject");

        var originPrefabActive = _prefab.gameObject.activeSelf;
        // Disable prefab before instantiation to prevent Awake from running during instantiation
        _prefab.gameObject.SetActive(false);

        var obj = Object.Instantiate(_prefab, Vector3.zero, Quaternion.identity);
#if UNITY_EDITOR
        //給spawner做？PoolObject第一次拿出來才需要？
        //Editor裡還是直接使用AutoAttributeManager，cache太容易髒掉了
        // Debug.Log($"AutoReferenceAllChildren: {obj.name}", obj);
        AutoAttributeManager.AutoReferenceAllChildren(obj.gameObject);
        //FIXME: 還要...?
#endif
        Object.DontDestroyOnLoad(obj);
        obj.SetBindingPool(_poolManager);
        SceneLifecycleManager.PreparePoolObjectImplementation(obj,
            _poolManager.GetComponent<WorldUpdateSimulator>());
        // Prepare implementation while object is disabled to control initialization order

        obj.gameObject.SetActive(true);
        //打開 開始跑Awake

        // obj.transform.SetParent(_poolManager.poolbjects);

        obj.gameObject.SetActive(false);

        obj.OriginalPrefab = _prefab;

        AllObjs.Add(obj);

        _prefab.gameObject.SetActive(originPrefabActive);

        DisabledObjs.Add(obj);

        if (updatePrewarm)
            UpdatePoolEntry();
        //
    }

    [Conditional("UNITY_EDITOR")]
    public void UpdatePoolEntry()
    {
        if (
            _bindingEntry.prefab.gameObject.scene != null
            && _bindingEntry.prefab.gameObject.scene.name != default
            && _bindingEntry.prefab.gameObject.scene.name != null
        )
        {
            PoolLogger.LogError(
                $"Update prewarm data failed: {_bindingEntry.prefab.gameObject.name}",
                _bindingEntry.prefab
            );

            return;
        }

        if (_bindingEntry.prefab.IsGlobalPool)
        {
            if (_poolManager.globalPrewarmDataLogger != null)
            {
                _poolManager.globalPrewarmDataLogger.UpdatePoolObjectEntry(
                    _bindingEntry.prefab,
                    AllObjs.Count
                );
                PoolLogger.LogInfo(
                    $"Update Global Pool Entry: {AllObjs.Count}",
                    _bindingEntry.prefab
                );
                return;
            }
        }
        else
        {
            if (_poolManager.prewarmDataLogger != null)
            {
                _poolManager.prewarmDataLogger.UpdatePoolObjectEntry(
                    _bindingEntry.prefab,
                    AllObjs.Count
                );
                PoolLogger.LogInfo(
                    $"Update Scene Pool Entry: {AllObjs.Count}",
                    _bindingEntry.prefab
                );
                return;
            }
        }

        // Note: Update prewarm data validation handled by caller
    }

    public void ReturnToPool(PoolObject obj)
    {
        // if (obj.busy)
        // return;
        if (OnUseObjs.Contains(obj))
        {
            obj.BeforeObjectReturnToPool(_poolManager);
            // if (obj.UnsolvedIssueBeforeDestroy <= 0)
            // {

            //歸還強制解除保護。
            obj.MarkAsRecyclable();
            OnUseObjs.Remove(obj);
            DisabledObjs.Insert(0, obj);

            // Reset parent to pool container if object was borrowed to a specific node
            if (obj.transform.parent != null)
                obj.transform.SetParent(_poolManager.poolbjects);

            obj.OnReturnToPool(_poolManager);
            obj.gameObject.SetActive(false);
        }
        else if (DisabledObjs.Contains(obj))
        {
            // Debug.LogWarning(obj.name + " already returned", obj.gameObject);
        }
        else
        {
            // Debug.LogWarning(obj.name + "is not recorded in pool manager... , should remove by someone else.",
            //     obj.gameObject);
            //Debug.LogError("WTF?");
        }
    }

    public void Init()
    {
        if (init)
            return;

        AllObjs = new List<PoolObject>();
        DisabledObjs = new List<PoolObject>();
        OnUseObjs = new HashSet<PoolObject>();
        //[Fusion] INetworkObjectProvider.AcquireInstance threw an exception for [Prefab 168]: UnityEngine.MissingReferenceException: The object of type 'PoolObject' has been destroyed but you are still trying to access it.

        if (_bindingEntry.prefab == null)
        {
            Debug.LogError("ObjectPool Init Error: prefab is null, maxcount:" +
                           _bindingEntry.DefaultMaximumCount);
            return;
        }

        // var prefabName = _bindingEntry.prefab.name;
        PoolLogger.LogDev(
            $"Create New Pool: {_bindingEntry.prefab.name} with {_bindingEntry.DefaultMaximumCount} objects"
        );
        // SetIsHandledPoolRequestPoolObject(_prefab, true);
        for (var i = 0; i < ObjectCount; i++)
            //關掉原型??
            AddAObject();

        init = true;
    }
}
