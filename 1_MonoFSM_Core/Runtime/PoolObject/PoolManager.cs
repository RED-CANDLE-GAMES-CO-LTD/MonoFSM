using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Auto.Utils;
using Cysharp.Threading.Tasks;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.AddressableAssets;
using MonoFSM.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public interface INativePool
{
    public void Clear();
}

public class PoolNativeObjectManager<T> where T : INativePool, new()
{
    public PoolNativeObjectManager(int prepareCount)
    {
        _objList = new List<T>();
        for (var i = 0; i < prepareCount; i++)
            _objList.Add(new T());
        // Debug.Log("PoolNativeObjectManager init" + typeof(T));
    }

    private int _index = 0;
    private readonly List<T> _objList;

    public T Borrow()
    {
        // Debug.Log("Borrow " + _objList.Count + ",index:" + _index);
        if (_index >= _objList.Count) _index = 0;
        return _objList[_index++];
    }

    public void Clear()
    {
        foreach (var _obj in _objList)
        {
            _obj.Clear();
        }
    }
}

public delegate void BeforeActiveHandler(PoolObject obj);

/// <summary>
/// FIXME: 重寫！
/// </summary>
public class PoolManager : SingletonBehaviour<PoolManager>
{
    public static void PreparePoolObjectImplementation(PoolObject obj)
    {
        if (obj.TryGetComponent<PrefabSerializeCache>(out var cache))
        {
            cache.RestoreReferenceCache();
        }
        else
        {
            AutoAttributeManager.AutoReferenceAllChildren(obj.gameObject); //
        }

        HandleGameLevelAwakeReverse(obj.gameObject);
        HandleGameLevelAwake(obj.gameObject);
        HandleGameLevelStartReverse(obj.gameObject);
        HandleGameLevelStart(obj.gameObject);
        LevelResetChildrenReload(obj.gameObject);
        obj.OnPrepare();
        // obj.PoolObjectResetAndStart();
    }

    public static void HandleGameLevelConfigSetting(MonoBehaviour level)
    {
        var ILevelConfigs = new List<ILevelConfig>(level.GetComponentsInChildren<ILevelConfig>(true));

        foreach (var item in ILevelConfigs)
        {
            if (item == null)
                continue;
            try
            {
                item.SetLevelConfig();
            }
            catch (Exception e)
            {
                if (item is MonoBehaviour)
                    Debug.LogError(e.StackTrace, item as MonoBehaviour);
                else
                    Debug.LogError(e.StackTrace);
            }
        }
    }

    //LevelReset, 重職關卡時，一換scene時
    //開放世界用不到？死掉復活？
    public  static void ResetReload(GameObject root)
    {
        //每次重置都要做的, LevelReset, LevelResetAfter?
        LevelResetChildrenReload(root);
        //大便！
        // HandleEnterLevelReset(root);
        //FIXME: 再重整一下
        LevelResetStart(root);
    }


    public static void OnBeforeDestroyScene(Scene s)
    {
        
        foreach (var g in s.GetRootGameObjects())
        {
            foreach (var rcgOnDestroy in g.GetComponentsInChildren<ISceneDestroy>(true))
            {
                try
                {
                    rcgOnDestroy.OnSceneDestroy();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

            }
        }
    }


    public static void LevelResetChildrenReload(GameObject gObj)
    {
        var levelResets = new List<IResetStateRestore>();
        gObj.GetComponentsInChildren(true, levelResets);
        levelResets.Reverse();
        foreach (var item in levelResets)
        {
            if (item == null)
                continue;
            try
            {
                item.ResetStateRestore();
            }
            catch (Exception e)
            {
                if (item is MonoBehaviour behaviour)
                    Debug.LogError(e.Message + "\n" + e.StackTrace, behaviour);
                else
                    Debug.LogError(e.Message + "\n" + e.StackTrace);
            }
        }
    }

    /// <summary>
    /// 摸別人，綁定事件
    /// </summary>
    /// <param name="gObj"></param>
    //最新規，九日沒在用？
    public static void LevelResetStart(GameObject gObj) //由下往上
    {
        var levelResets = new List<IResetStart>();
        gObj.GetComponentsInChildren(true, levelResets);
        levelResets.Reverse();
        foreach (var item in levelResets)
        {
            if (item == null)
                continue;
            // try
            // {
            item.ResetStart();
            // }
            // catch (Exception e)
            // {
            //     if (item is MonoBehaviour behaviour)
            //         Debug.LogError(e.Message + "\n" + e.StackTrace, behaviour);
            //     else
            //         Debug.LogError(e.Message + "\n" + e.StackTrace);
            // }
        }
    }

    public static void HandleGameLevelAwake(GameObject level) //FIXME: 不要放這？
    {
        var levelAwakes = new List<ISceneAwake>(level.GetComponentsInChildren<ISceneAwake>(true));
        // ILevelAwakes.Reverse();
        foreach (var item in levelAwakes)
        {
            if (item == null)
                continue;
            Profiler.BeginSample(item.ToString());
            try
            {
                item.EnterSceneAwake();
            }
            catch (Exception e)
            {
                if (item is MonoBehaviour behaviour)
                    Debug.LogError(e.StackTrace, behaviour);
                else
                    Debug.LogError(e.StackTrace);
            }

            Profiler.EndSample();
        }
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


    public static void HandleGameLevelAwakeReverse(GameObject level)
    {
        var ILevelAwakes = new List<ISceneAwakeReverse>(level.GetComponentsInChildren<ISceneAwakeReverse>(true));
        ILevelAwakes.Reverse();
        foreach (var item in ILevelAwakes)
        {
            if (item == null)
                continue;
            Profiler.BeginSample(item.ToString());
            try
            {
                item.EnterSceneAwakeReverse();
            }
            catch (Exception e)
            {
                if (item is MonoBehaviour)
                    Debug.LogError(e.StackTrace, item as MonoBehaviour);
                else
                    Debug.LogError(e.StackTrace);
            }

            Profiler.EndSample();
        }
    }

    public static void HandleGameLevelStart(GameObject level)
    {
        var levelStarts = new List<ISceneStart>(level.GetComponentsInChildren<ISceneStart>(true));

        //巢狀RCGArgEventBinder  要從下面往上組
        // ILevelStarts.Reverse();

        foreach (var item in levelStarts)
        {
            if (item == null)
                continue;
            try
            {
                item.EnterSceneStart();
            }
            catch (Exception e)
            {
                if (item is MonoBehaviour)
                    Debug.LogError(e.Message + "\n" + e.StackTrace, item as MonoBehaviour);
                else
                    Debug.LogError(e.StackTrace);
            }
        }
    }

    public static void HandleGameLevelStartReverse(GameObject level)
    {
        var ILevelStarts = new List<ISceneStartReverse>(level.GetComponentsInChildren<ISceneStartReverse>(true));

        //巢狀RCGArgEventBinder  要從下面往上組
        ILevelStarts.Reverse();

        foreach (var item in ILevelStarts)
        {
            if (item == null)
                continue;
            try
            {
                item.EnterSceneStartReverse();
            }
            catch (Exception e)
            {
                if (item is MonoBehaviour)
                    Debug.LogError(e.StackTrace, item as MonoBehaviour);
                else
                    Debug.LogError(e.StackTrace);
            }
        }
    }

    // public bool IsReady = false;
    [Header("PrewarmData Logger")] public Transform poolbjects;
    public PoolPrewarmData prewarmDataLogger;
    public PoolPrewarmData globalPrewarmDataLogger;

    /*  public void RegisterPoolRequest(MonoBehaviour requester, GameObject prefab, int count = 1)
      {
          if (prefab == null)
              return;

          PoolObject poolObject = prefab.GetComponent<PoolObject>();

          if (poolObject != null && count > 0)
              records.Add(new PoolObjectRequestRecords(requester, poolObject, count));

      }*/

    /*public void RegisterPoolRequest(MonoBehaviour requester, PoolObject prefab, int count = 1)
    {

        if (prefab == null)
            return;

        if (count == 0)
            return;

        records.Add(new PoolObjectRequestRecords(requester, prefab, count));

    }*/

    public void RegisterPoolPrewarmData(MonoBehaviour requester, PoolPrewarmData data)
    {
        if (data == null)
            return;

        foreach (var entry in data.objectEntries)
            records.Add(new PoolObjectRequestRecords(requester, entry.prefab,
                entry.DefaultMaximumCount));
    }


    public class PoolObjectRequestRecords
    {
        public PoolObjectRequestRecords(MonoBehaviour requester, PoolObject prefab, int count)
        {
            _requester = requester;
            _prefab = prefab;
            _count = count;
        }

        public void Clear()
        {
            _requester = null;
            _prefab = null;
            _count = 0;
        }

        public MonoBehaviour _requester;
        public PoolObject _prefab;
        public int _count = 0;
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

    [System.Serializable]
    public class PoolObjectEntry
    {
        public PoolObject prefab;
        public int DefaultMaximumCount = 1;

        public void Clear()
        {
            prefab = null;
            DefaultMaximumCount = 0;
        }
    }

    [System.Serializable]
    public class AddressableEntry
    {
        public AssetReference _assetReference;
        public GameObject _prefab;

        public AddressableEntry(AssetReference assetReference, GameObject prefab)
        {
            _prefab = prefab;
            _assetReference = assetReference;
        }
    }

    private List<PoolObjectEntry> PoolObjectEntries;

    [Header("Run Time Data")] public Dictionary<PoolObject, ObjectPool> PoolDictionary;
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
    }

    //
    //private bool _poolCreated = false;

    public GameObject BorrowOrInstantiate(GameObject obj, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null, Action<PoolObject> handler = null)
    {
        var hasRequest = obj.TryGetComponent(out PoolRequest request);
        var hasPoolObj = obj.TryGetComponent<PoolObject>(out var poolObj);

        if (hasRequest)
        {
            return Borrow(request.PoolObjectRequests.prefab, position, rotation, parent, handler).gameObject;
        }
        else if (hasPoolObj)
        {
            return Borrow(poolObj, position, rotation, parent, handler).gameObject;
        }
        else
        {
            Debug.LogError("RunTime Instantiate");
            return Instantiate(obj, position, rotation, parent);
        }
    }

    public async UniTask<GameObject> BorrowOrInstantiateRcgAssetReference(RCGAssetReference obj,
        Vector3 position = default, Quaternion rotation = default,
        Transform parent = null, Action<PoolObject> handler = null)
    {
        GameObject poolObject = null;
        if (prewarmDataLogger != null)
        {
            poolObject = prewarmDataLogger.TryFindPrefab(obj.AssetReference);

            if (poolObject != null)
                return BorrowOrInstantiate(poolObject, position, rotation, parent, handler);
        }


#if UNITY_EDITOR
        Debug.LogError("Please Prewarm this:" + obj.editorAsset, obj.editorAsset);
#endif
        Debug.LogError("Please Prewarm this:" + obj.AssetReference.AssetGUID);

        if (allLoadedRCGRefereces.Contains(obj) == false)
        {
            allLoadedRCGRefereces.Add(obj);
        }

        poolObject = await obj.GetAssetAsync<GameObject>();

        if (this.prewarmDataLogger != null)
            this.prewarmDataLogger.RegisterEntry(poolObject, obj.AssetReference);

        return BorrowOrInstantiate(poolObject, position, rotation, parent, handler);

        return null;
    }

    public List<RCGAssetReference> allLoadedRCGRefereces = new List<RCGAssetReference>();

    public T BorrowOrInstantiate<T>(T obj, Transform parent) where T : MonoBehaviour
    {
        return BorrowOrInstantiate(obj, Vector3.zero, Quaternion.identity, parent);
    }

    public T BorrowOrInstantiate<T>(T obj, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null, Action<PoolObject> handler = null) where T : MonoBehaviour
    {
        if (obj == null)
        {
            Debug.LogError("BorrowOrInstantiate: obj is null");
            return null;
        }

        if (obj.TryGetComponent<PoolObject>(out var poolObj))
        {
            return Borrow(poolObj, position, rotation, parent, handler).GetComponent<T>();
        }
        else
        {
            Debug.LogError("It's not a pool object...", obj);
            return Instantiate(obj, position, rotation, parent);
        }
    }

    private PoolObject Borrow(PoolObject prefab, Vector3 position, Quaternion rotation, Transform parent = null,
        Action<PoolObject> handler = null)
    {
        //FIXME: 這很糟
        // if (IsReady == false) Debug.LogError("太早跟pool拿東西了，危險。" + prefab, prefab);


        if (prefab.UseSceneAsPool)
        {
            prefab.TransformReset();
            //FIXME:這裡又跑一次...
            prefab.PoolObjectResetAndStart();

            var transform1 = prefab.transform;
            transform1.parent = parent;
            transform1.rotation = rotation;
            transform1.position = position;


            prefab.OnBorrowFromPool(null); //OnPoolReset

            // 這會影響設定黨 樹上有結構

            //FIXME: 為什麼要做這件事？？

            //先reset, 後面才
            prefab.OverrideTransformSetting(position, rotation, parent, prefab.transform.localScale);
            prefab.TransformReset();

            prefab.gameObject.SetActive(true);
            // prefab.ResetAnim();
            //FIXME:這裡又跑一次...
            prefab.PoolObjectResetAndStart();

            handler?.Invoke(prefab); //TODO: 這個比較後面call的？

            // Debug.Log("Use Scene As Pool");

            return prefab;
        }
        // prefab

        if (!PoolDictionary.ContainsKey(prefab))
        {
#if RCG_DEV
            //FIXME: 先拿掉的註解
            // Debug.LogError("PoolManager: " + prefab.name + " is not in the pool dictionary");
#endif
            AddAPool(prefab);
            PoolDictionary[prefab].UpdatePoolEntry();
        }

        return PoolDictionary[prefab].Borrow(position, rotation, parent, handler);
    }

    public void ReturnToPool(PoolObject prefab)
    {
        PoolDictionary[prefab.OriginalPrefab].ReturnToPool(prefab);
    }

    //FIXME: 
    public void ReturnToPool(MonoPoolObj obj)
    {
        // PoolDictionary[obj.OriginalPrefab].ReturnToPool(prefab);
        throw new NotImplementedException("ReturnToPool for MonoPoolObj is not implemented yet.");
    }

    //FIXME: 有可能保留AG_S2的pool, 或是在特定scene不做清除之類的動作
    public void ReCalculatePools()
    {
        Profiler.BeginSample("ReCalculatePools");

        //把null game level 清掉了
        ReCalculatePoolObjectEntries();

        Profiler.BeginSample("DestroyNonUsedPool");
        for (var i = allPools.Count - 1; i >= 0; i--)
        {
            var currentPool = allPools[i];
            //FIXME: 同一個景重load!????
            var entry = isInRequest(currentPool._prefab);

            //移除沒用到的pool
            if (entry == null)
            {
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

    public PoolObjectEntry isInRequest(PoolObject prefab)
    {
        for (var i = PoolObjectEntries.Count - 1; i >= 0; i--)
            if (PoolObjectEntries[i].prefab == prefab)
                return PoolObjectEntries[i];

        return null;
    }


    public class ObjectPool
    {
        public ObjectPool(PoolObjectEntry bindingEntry, PoolManager manager)
        {
            _bindingEntry = bindingEntry;
            ObjectCount = bindingEntry.DefaultMaximumCount;
            _prefab = bindingEntry.prefab;
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

        public void ReturnAllObjects()
        {
            var StillOnUses = new List<PoolObject>();
            StillOnUses.AddRange(OnUseObjs);

            for (var i = 0; i < StillOnUses.Count; i++) StillOnUses[i].ReturnToPool();
        }

        public void ReturnAllObjects(Scene scene)
        {
            var StillOnUses = new List<PoolObject>();
            StillOnUses.AddRange(OnUseObjs);
            StillOnUses = StillOnUses.FindAll((a) => a.gameObject.scene == scene);

            for (var i = 0; i < StillOnUses.Count; i++) StillOnUses[i].ReturnToPool();
        }


        public void DestroyPool()
        {
            foreach (var obj in AllObjs)
                if (obj && obj.gameObject)
                    Destroy(obj.gameObject);
                else
                    Debug.LogWarning("[Warning]" + _prefab.gameObject.name + " is destroyed????");

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
            // OnUseObjs.RemoveAllNull();
            AllObjs.RemoveAllNull();
            DisabledObjs.RemoveAllNull();
            ReturnAllObjects();

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
                for (var i = 0; i < offset; i++) AddAObject();
            }
            else if (AllObjs.Count > _bindingEntry.DefaultMaximumCount)
            {
                var offset = AllObjs.Count - _bindingEntry.DefaultMaximumCount;
#if RCG_DEV
                // Debug.Log(_bindingEntry.prefab+":AllObjs.Count > _bindingEntry.DefaultMaximumCount:"+_bindingEntry.DefaultMaximumCount);
#endif
                for (var i = 0; i < offset; i++)
                {
                    Destroy(AllObjs[i].gameObject);
                    AllObjs[i] = null;
                }

                AllObjs.RemoveAllNull();
            }


            OnUseObjs.Clear();
            DisabledObjs.Clear();
            DisabledObjs.AddRange(AllObjs);
        }

        public PoolObject Borrow(Vector3 position, Quaternion rotation, Transform parent = null,
            Action<PoolObject> beforeHandler = null)
        {
            if (DisabledObjs.Count == 0)
            {
                //FIXME: 先拿掉的註解
                // Debug.LogError(
                //     "[Pool Manager]" + _prefab.gameObject.name + " Pool Bankrupt" + "OnUsed:" + OnUseObjs.Count,
                //     _prefab);
                AddAObject(true);
            }


            if (DisabledObjs.Count > 0)
            {
                var obj = DisabledObjs[0];
                DisabledObjs.RemoveAt(0);
                OnUseObjs.Add(obj);

                obj.OnBorrowFromPool(_poolManager); //OnPoolReset

                // 這會影響設定黨 樹上有結構

                //FIXME: 為什麼要做這件事？？
                obj.OverrideTransformSetting(position, rotation, parent, obj.OriginalPrefab.transform.localScale);
                obj.TransformReset();


                beforeHandler?.Invoke(obj);

                obj.gameObject.SetActive(true);

                //這裡才是真的onBorrow
                obj.PoolObjectResetAndStart();


                return obj;
            }
            else
            {
                Debug.LogError("[Pool Manager]" + _prefab.gameObject.name + " Pool Bankrupt");
                return null;
            }
        }

        public void AddAObject(bool updatePrewarm = false)
        {
            if (_poolManager == null)
                Debug.LogError("What?");

            var originPrefabActive = _prefab.gameObject.activeSelf;
            _prefab.gameObject.SetActive(false); //FIXME: 為什麼prefab instantiate前需要關著？？ 
            //因為開著他會跑Awake 關起來才不會跑

            var obj = Instantiate(_prefab, Vector3.zero, Quaternion.identity);
            DontDestroyOnLoad(obj);
            obj.SetBindingPool(_poolManager);
            PreparePoolObjectImplementation(obj);
            //FIXME: 為什麼要關著prepare? 
            //這邊會跑auto

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
            if (_bindingEntry.prefab.gameObject.scene != null &&
                _bindingEntry.prefab.gameObject.scene.name != default &&
                _bindingEntry.prefab.gameObject.scene.name != null)
            {
                Debug.LogError("Update Pre warm Data Failed :" + _bindingEntry.prefab.gameObject.name);

                return;
            }

            if (_bindingEntry.prefab.IsGlobalPool)
            {
                if (_poolManager.globalPrewarmDataLogger != null)
                {
                    _poolManager.globalPrewarmDataLogger.UpdatePoolObjectEntry(_bindingEntry.prefab, AllObjs.Count);
                    Debug.LogError("Update Global Pool Entry" + AllObjs.Count, _bindingEntry.prefab);
                    return;
                }
            }
            else
            {
                if (_poolManager.prewarmDataLogger != null)
                {
                    _poolManager.prewarmDataLogger.UpdatePoolObjectEntry(_bindingEntry.prefab, AllObjs.Count);
                    Debug.LogError("Update Scene Pool Entry" + AllObjs.Count, _bindingEntry.prefab);
                    return;
                }
            }

            //FIXME: 這裡有問題
            // Debug.LogError("Update Pre warm Data Failed :" + _bindingEntry.prefab.gameObject.name,
            //     _bindingEntry.prefab.gameObject);
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
                OnUseObjs.Remove(obj);
                DisabledObjs.Insert(0, obj);

                //FIXME: 
                if (obj.transform.parent != null) //有被借到某個特定node才
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
            if (init) return;

            AllObjs = new List<PoolObject>();
            DisabledObjs = new List<PoolObject>();
            OnUseObjs = new HashSet<PoolObject>();

#if RCG_DEV
            Debug.Log(
                "[PoolManager] Create New Pool: " + _bindingEntry.prefab + ":AllObjs.Count " +
                _bindingEntry.DefaultMaximumCount);
#endif
            // SetIsHandledPoolRequestPoolObject(_prefab, true);
            for (var i = 0; i < ObjectCount; i++)
                //關掉原型??
                AddAObject();

            //TODO: PoolRequest給場上的東西？？
            init = true;
        }
    }
}