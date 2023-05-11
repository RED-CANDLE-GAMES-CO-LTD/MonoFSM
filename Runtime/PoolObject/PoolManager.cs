using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolNativeObjectManager<T> where T : new()
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
}

public delegate void BeforeActiveHandler(PoolObject obj);

public class PoolManager : SingletonBehaviour<PoolManager>
{
    public bool IsReady = false;
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

        for (var i = 0; i < data.objectEntries.Count; i++)
            records.Add(new PoolObjectRequestRecords(requester, data.objectEntries[i].prefab,
                data.objectEntries[i].DefaultMaximumCount));
    }


    public class PoolObjectRequestRecords
    {
        public PoolObjectRequestRecords(MonoBehaviour requester, PoolObject prefab, int count)
        {
            _requester = requester;
            _prefab = prefab;
            _count = count;
        }

        public MonoBehaviour _requester;
        public PoolObject _prefab;
        public int _count = 0;
    }

    private void ReCalculatePoolObjectEntries()
    {
        records.RemoveAll(e => e._requester == null);
        PoolObjectEntries.Clear();

        for (var i = 0; i < records.Count; i++) AddEntry(PoolObjectEntries, records[i]._prefab, records[i]._count);
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

        CreatePools();
    }

    //
    private bool _poolCreated = false;

    public GameObject BorrowOrInstantiate(GameObject obj, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null, BeforeActiveHandler handler = null)
    {
        var hasRequest = obj.TryGetComponent<PoolRequest>(out PoolRequest request);
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

    public T BorrowOrInstantiate<T>(T obj, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null, BeforeActiveHandler handler = null) where T : MonoBehaviour
    {
        var poolObj = obj.GetComponent<PoolObject>();
        if (poolObj != null)
        {
            return Borrow(poolObj, position, rotation, parent, handler).GetComponent<T>();
        }
        else
        {
            Debug.LogError("It's not a pool object...");
            return Instantiate(obj, position, rotation, parent);
        }
    }

    private PoolObject Borrow(PoolObject prefab, Vector3 position, Quaternion rotation, Transform parent = null,
        BeforeActiveHandler handler = null)
    {
        if (IsReady == false) Debug.LogError("太早跟pool拿東西了，危險。" + prefab, prefab);


        if (prefab.UseSceneAsPool)
        {
            prefab.TransformReset();
            prefab.PoolObjecResetAndStart();

            prefab.transform.parent = parent;
            prefab.transform.rotation = rotation;
            prefab.transform.position = position;

            prefab.gameObject.SetActive(true);
            prefab.ResetAnim();

            Debug.Log("Use Scene As Pool");

            return prefab;
        }
        // prefab

        if (!PoolDictionary.ContainsKey(prefab))
        {
            AddAPool(prefab);
            PoolDictionary[prefab].UpdatePoolEntry();
        }

        return PoolDictionary[prefab].Borrow(position, rotation, parent, handler);
    }

    public void ReturnToPool(PoolObject prefab)
    {
        PoolDictionary[prefab.OriginalPrefab].ReturnToPool(prefab);
    }


    public void CreatePools()
    {
        if (_poolCreated)
            return;

        for (var i = 0; i < PoolObjectEntries.Count; i++)
        {
            var pool = new ObjectPool(PoolObjectEntries[i], this);
            allPools.Add(pool);
            PoolDictionary.Add(PoolObjectEntries[i].prefab, pool);
        }

        for (var i = 0; i < allPools.Count; i++) allPools[i].Init();

        _poolCreated = true;
    }

    public void ReCalculatePools()
    {
        if (!_poolCreated)
            return;

        ReCalculatePoolObjectEntries();

        for (var i = 0; i < allPools.Count; i++)
        {
            var currentPool = allPools[i];
            //FIXME: 同一個景重load!????
            var entry = isInRequest(currentPool._prefab);

            //移除沒用到的pool
            if (entry == null)
            {
                allPools[i].DestroyPool();
                allPools[i] = null;
            }
            else
            {
                allPools[i]._bindingEntry = entry;
            }
        }

        allPools.RemoveAll(e => e == null);

        //增加新的pool
        for (var i = 0; i < PoolObjectEntries.Count; i++)
        {
            var entry = PoolObjectEntries[i];
            if (!PoolDictionary.ContainsKey(entry.prefab))
            {
                var pool = new ObjectPool(PoolObjectEntries[i], this);
                allPools.Add(pool);
                pool.Init();
            }
        }

        PoolDictionary.Clear();

        //重建Dictionary
        for (var i = 0; i < allPools.Count; i++) PoolDictionary.Add(allPools[i]._prefab, allPools[i]);


        var sw = new System.Diagnostics.Stopwatch();

        sw.Start();

        for (var i = 0; i < allPools.Count; i++) allPools[i].ScalePoolToNewMaximum();

        sw.Stop();
        Debug.Log("[PoolManager] Prepare ElapsedMilliseconds:" + sw.ElapsedMilliseconds);
        // UnityEngine.Debug.LogFormat("[Auto] Assigned <color={5}><b>{4}/{2}</b></color> [Auto*] variables in <color=#cc3300><b>{3} Milliseconds </b></color> - Analized {0} MonoBehaviours and {1} variables",
        //    monoBehavioursInSceneWithAuto.Count(), variablesAnalized, variablesWithAuto, sw.ElapsedMilliseconds, autoVarialbesAssigned_count, autoVarialbesAssigned_count + autoVarialbesNotAssigned_count, result_color);
    }

    public void ReturnAllObjects()
    {
        for (var i = 0; i < allPools.Count; i++) allPools[i].ReturnAllObjects();
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
        for (var i = 0; i < PoolObjectEntries.Count; i++)
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
        public List<PoolObject> OnUseObjs;
        public List<PoolObject> DisabledObjs;

        public PoolObject _prefab;

        private bool init = false;

        public void ReturnAllObjects()
        {
            var StillOnUses = new List<PoolObject>();
            StillOnUses.AddRange(OnUseObjs);

            for (var i = 0; i < StillOnUses.Count; i++) StillOnUses[i].ReturnToPool();
        }

        public void DestroyPool()
        {
            for (var i = 0; i < AllObjs.Count; i++)
                if (AllObjs[i] && AllObjs[i].gameObject)
                    Destroy(AllObjs[i].gameObject);
                else
                    Debug.LogWarning("[Warning]" + _prefab.gameObject.name + " is destroyed????");

            AllObjs.Clear();
            OnUseObjs.Clear();
            DisabledObjs.Clear();
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
            OnUseObjs.RemoveAll(e => e == null);
            AllObjs.RemoveAll(e => e == null);
            DisabledObjs.RemoveAll(e => e == null);
            ReturnAllObjects();

            if (AllObjs.Count == _bindingEntry.DefaultMaximumCount)
            {
                return;
            }
            else if (AllObjs.Count < _bindingEntry.DefaultMaximumCount)
            {
                var offset = _bindingEntry.DefaultMaximumCount - AllObjs.Count;
                // SetIsHandledPoolRequestPoolObject(_prefab, true);

                for (var i = 0; i < offset; i++) AddAObject();

                // SetIsHandledPoolRequestPoolObject(_prefab, false);
            }
            else if (AllObjs.Count > _bindingEntry.DefaultMaximumCount)
            {
                var offset = AllObjs.Count - _bindingEntry.DefaultMaximumCount;

                for (var i = 0; i < offset; i++)
                {
                    Destroy(AllObjs[i].gameObject);
                    AllObjs[i] = null;
                }

                AllObjs.RemoveAll(e => e == null);
            }


            OnUseObjs.Clear();
            DisabledObjs.Clear();
            DisabledObjs.AddRange(AllObjs);
        }

        public bool CanBorrow()
        {
            return DisabledObjs.Count > 0;
        }

        public PoolObject Borrow(Vector3 position, Quaternion rotation, Transform parent = null,
            BeforeActiveHandler beforeHandler = null)
        {
            if (DisabledObjs.Count == 0)
                AddAObject();

            if (DisabledObjs.Count > 0)
            {
                var obj = DisabledObjs[0];
                DisabledObjs.RemoveAt(0);
                OnUseObjs.Add(obj);

                obj.OnBorrowFromPool(_poolManager); //OnPoolReset

                // 這會影響設定黨 樹上有結構

                obj.OverrideTransformSetting(position, rotation, parent, obj.OriginalPrefab.transform.localScale);
                obj.TransformReset();


                beforeHandler?.Invoke(obj);

                obj.gameObject.SetActive(true);


                obj.PoolObjecResetAndStart();


                return obj;
            }
            else
            {
                Debug.LogError("[Pool Manager]" + _prefab.gameObject.name + " Pool Bankrupt");
                return null;
            }
        }

        public void AddAObject()
        {
            if (_poolManager == null)
                Debug.LogError("What?");

            var originPrefabActive = _prefab.gameObject.activeSelf;
            _prefab.gameObject.SetActive(false); //FIXME: 為什麼prefab instantiate前需要關著？？ 
            //因為開著他會跑Awake 關起來才不會跑

            var obj = Instantiate(_prefab, Vector3.zero, Quaternion.identity);
            obj.OnPrepare(); //FIXME: 為什麼要關著prepare? 
            //這邊會跑auto

            obj.gameObject.SetActive(true);
            //打開 開始跑Awake

            obj.transform.SetParent(_poolManager.poolbjects);
            obj.gameObject.SetActive(false);

            obj.OriginalPrefab = _prefab;
            obj.SetBindingPool(_poolManager);
            AllObjs.Add(obj);

            _prefab.gameObject.SetActive(originPrefabActive);


            DisabledObjs.Add(obj);
            UpdatePoolEntry();
            //
        }

        public void UpdatePoolEntry()
        {
            if (_bindingEntry.prefab.gameObject.scene != null &&
                _bindingEntry.prefab.gameObject.scene.name != default &&
                _bindingEntry.prefab.gameObject.scene.name != null)
            {
                Debug.Log("Update PrewarmData Failed :" + _bindingEntry.prefab.gameObject.name);

                return;
            }


            if (_bindingEntry.prefab.IsGlobalPool)
            {
                if (_poolManager.globalPrewarmDataLogger != null)
                    _poolManager.globalPrewarmDataLogger.UpdatePoolObjectEntry(_bindingEntry.prefab, AllObjs.Count);
            }
            else
            {
                if (_poolManager.prewarmDataLogger != null)
                    _poolManager.prewarmDataLogger.UpdatePoolObjectEntry(_bindingEntry.prefab, AllObjs.Count);
            }
        }


        public void ReturnToPool(PoolObject obj)
        {
            if (obj.busy)
                return;

            if (OnUseObjs.Contains(obj))
            {
                obj.BeforeObjectReturnToPool(_poolManager);
                // if (obj.UnsolvedIssueBeforeDestroy <= 0)
                // {
                OnUseObjs.Remove(obj);
                DisabledObjs.Insert(0, obj);
                obj.transform.SetParent(_poolManager.poolbjects);
                obj.OnReturnToPool(_poolManager);
                // }
                // else //FIXME:應該沒需要了
                // {
                //     obj.busy = true;
                //     // PromiseManager.Instance.MakePromise().AddCondition(() => obj.UnsolvedIssueBeforeDestroy <= 0).OnComlete(() =>
                //     // {
                //     OnUseObjs.Remove(obj);
                //     DisabledObjs.Add(obj);
                //     obj.transform.SetParent(_poolManager.poolbjects);
                //     obj.OnReturnToPool(_poolManager);
                //     obj.busy = false;
                //     // }).SetOnce();
                // }


                obj.gameObject.SetActive(false);
            }
            else if (DisabledObjs.Contains(obj))
            {
                Debug.LogWarning(obj.name + " already returned", obj.gameObject);
            }
            else
            {
                Debug.LogWarning(obj.name + "is not recorded in pool manager... , should remove by someone else.",
                    obj.gameObject);
                //Debug.LogError("WTF?");
            }
        }

        public void Init()
        {
            if (init) return;

            AllObjs = new List<PoolObject>();
            DisabledObjs = new List<PoolObject>();
            OnUseObjs = new List<PoolObject>();

            // SetIsHandledPoolRequestPoolObject(_prefab, true);
            for (var i = 0; i < ObjectCount; i++)
                //關掉原型??
                AddAObject();

            //TODO: PoolRequest給場上的東西？？
            init = true;
        }
    }
}