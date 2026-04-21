using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

public interface IPoolObject : IResetter
{
    void PoolOnReturnToPool();
    void PoolOnPrepared(PoolObject poolObj);
    void PoolBeforeReturnToPool(); //gameObject還沒關，可以set動畫之類的
}

public interface IPoolBorrowOnEnable
{
    void OnBorrowFromPoolOnEnable();
}

public interface IFXPlayerOwner //hmm
{
    bool IsActive { get; }
}

public interface IPoolObjectPlayer
{
    // IFXPlayerOwner Owner { get; }
}

[RequireComponent(typeof(MonoObj))]
[DisallowMultipleComponent]
public class
    PoolObject : MonoBehaviour, ISceneAwake, IPoolableObject //可以順便管 active state?
{
    // public MonoReferenceCache _monoReferenceCache; //要是prefab asset才需要

    [BoxGroup("誰噴的")]
    [ShowInPlayMode]
    public IPoolObjectPlayer lastPlayer;

#if UNITY_EDITOR
    [ShowInPlayMode]
    [NonSerialized]
    public string _lastPlayerName;
#endif

    [BoxGroup("誰噴的")]
    [PropertyOrder(-1)]
    [ShowInPlayMode]
    public Component lastPlayerComponent => lastPlayer as Component;

    [Button("Find fx to assign")]
    void Find()
    {
        this.SetFilterForAssignPrefab();
    }

    // [BoxGroup("Pool 測試工具")]
    // [Button("借用到場景", ButtonSizes.Medium)]
    // [ShowIf("@!UnityEngine.Application.isPlaying || !this.isOnScene")]
    // [EnableIf("@UnityEngine.Application.isPlaying")]
    // private void TestBorrowToScene()
    // {
    //     if (!Application.isPlaying)
    //     {
    //         PoolLogger.LogWarning("借用功能只能在 Runtime 時使用", this);
    //         return;
    //     }
    //
    //     if (PoolManager.Instance == null)
    //     {
    //         PoolLogger.LogError("找不到 PoolManager", this);
    //         return;
    //     }
    //
    //     // 在攝影機前方生成物件
    //     Vector3 spawnPosition = Vector3.zero;
    //     if (Camera.main != null)
    //     {
    //         spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
    //     }
    //
    //     var borrowedObj = PoolManager.Instance.BorrowOrInstantiate(
    //         OriginalPrefab != null ? OriginalPrefab : this,
    //         spawnPosition,
    //         Quaternion.identity,
    //         null,
    //         (poolObj) =>
    //         {
    //             PoolLogger.LogInfo($"成功借用物件: {poolObj.name}", poolObj);
    //         }
    //     );
    //
    //     if (borrowedObj != null)
    //     {
    //         PoolLogger.LogInfo($"借用物件到場景: {borrowedObj.name} 位置: {spawnPosition}", this);
    //     }
    // }

    [BoxGroup("池測試工具")]
    [Button("歸還到池", ButtonSizes.Medium)]
    [ShowIf("@UnityEngine.Application.isPlaying && this.isOnScene")]
    [EnableIf("@UnityEngine.Application.isPlaying && this.isOnScene")]
    private void TestReturnToPool()
    {
        if (!Application.isPlaying)
        {
            PoolLogger.LogWarning("歸還功能只能在 Runtime 時使用", this);
            return;
        }

        if (!this.isOnScene)
        {
            PoolLogger.LogWarning("物件不在場景中，無法歸還", this);
            return;
        }

        PoolLogger.LogInfo($"準備歸還物件到池: {this.name}", this);
        this.Recycle();
    }

    [BoxGroup("池測試工具")]
    [Button("切換保護狀態", ButtonSizes.Small)]
    [ShowIf("@UnityEngine.Application.isPlaying && this.isOnScene")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void TestToggleProtection()
    {
        if (!Application.isPlaying)
        {
            PoolLogger.LogWarning("保護狀態切換只能在 Runtime 時使用", this);
            return;
        }

        if (this.IsProtected())
        {
            this.MarkAsRecyclable();
            PoolLogger.LogInfo($"物件 {this.name} 已設為可回收", this);
        }
        else
        {
            this.MarkAsProtected();
            PoolLogger.LogInfo($"物件 {this.name} 已設為保護狀態", this);
        }
    }

    [BoxGroup("池測試工具")]
    [ShowInInspector]
    [ReadOnly]
    [ShowIf("@UnityEngine.Application.isPlaying")]
    private string TestStatus => GetTestStatus();

    private string GetTestStatus()
    {
        if (!Application.isPlaying)
            return "請進入 Play Mode 使用測試功能";

        var status = new StringBuilder();
        status.AppendLine($"物件狀態: {(this.isOnScene ? "在場景中" : "在池中")}");
        status.AppendLine($"保護狀態: {(this.IsProtected() ? "Protected" : "Recyclable")}");
        status.AppendLine($"來自池: {(this.IsFromPool ? "是" : "否")}");

        if (this.IsFromPool && this._bindingPoolManager != null)
        {
            status.AppendLine($"綁定管理器: {this._bindingPoolManager.name}");
        }

        return status.ToString();
    }

    public bool IsGlobalPool;

    [BoxGroup("延遲自動回收")]
    [Tooltip("啟用後，物件 Spawn 到場景後會在指定時間自動回收")]
    [SerializeField]
    private bool _enableAutoRecycle;

    [BoxGroup("延遲自動回收")]
    [ShowIf("_enableAutoRecycle")]
    [Tooltip("自動回收的延遲時間（秒）")]
    [Min(0.01f)]
    [SerializeField]
    private float _autoRecycleDelay = 3f;

    [BoxGroup("延遲自動回收")]
    [ShowIf("_enableAutoRecycle")]
    [ShowInInspector]
    [ReadOnly]
    private float _autoRecycleRemainingTime;

    private CancellationTokenSource _autoRecycleCts;

    public enum ProtectionState
    {
        Recyclable, // 可回收狀態，可以被池管理器回收
        Protected // 受保護狀態，不會被強制回收
        ,
    }

    [Header("物件保護管理")]
    [HideInInspector]
    public ProtectionState CurrentProtectionState = ProtectionState.Recyclable;

    public enum ShootFrom
    {
        HitData = 0,
        FxPlayer = 1,
        HitDataReceiverCenter = 2,
    }

    private void OnDisable()
    {
        this.Log("OnDisable");
    }

    [Header("決定要跟FXPlayer還是HitData(Receiver)的位置")]
    public ShootFrom InitPosType = ShootFrom.HitData; // 決定初始位置的來源

    [HideInInspector]
    public PoolObject OriginalPrefab;

    // IPoolableObject interface properties
    PoolObject IPoolableObject.OriginalPrefab => OriginalPrefab;

    // private bool _onUse = false;

    [HideInInspector]
    public PoolManager _bindingPoolManager;

    public PoolObjEvent OnReturnEvent = new PoolObjEvent();

    public void SetBindingPool(PoolManager manager)
    {
        _bindingPoolManager = manager;
    }

    private List<IPoolObject> IPoolObjectList = new();
    private List<IResetter> IResetterList = new();

    [AutoChildren]
    private IClearReference[] _iClearReferenceRefs;

    [AutoChildren]
    private IPoolObject[] _iPoolObjectRefs;

    [AutoChildren]
    private IResetter[] _iResetterRefs;

    [PreviewInInspector]
    [AutoChildren]
    private IPoolBorrowOnEnable[] IPoolBorrowedList;
    private bool inited = false;

    [PreviewInInspector]
    private List<AnimatorResetter> animResetters = new();

    [PreviewInInspector]
    private bool _animResetterInited = false;

    private void InitAnimResetters() // 初始化動畫重置器，只需執行一次
    {
        if (_animResetterInited)
            return;

        if (_anims == null)
        {
            // Debug.LogError("Anims == null?",this.gameObject);
            return;
        }

        _animResetterInited = true;

        foreach (var animator in _anims)
        {
            animResetters.Add(new AnimatorResetter(animator));
        }

        // Debug.Log("[PoolObjectResetAndStart] animResetters", this);
    }

    private void OnEnable()
    {
        // Note: Object preparation is handled by PoolManager during spawn process
    }

    // private void OnEnable() //從poolObject拿出來要確定動畫有重置，因為有人很壞，還沒開就被call Reset and Start
    // {
    //     if (needResetAnim == false)
    //         return;
    //     ResetAnim();
    // }
    // public void ResetAnim()
    // {
    //     // if (_animResetterInited == false)
    //     //     return;
    //     //
    //     // if (isActiveAndEnabled == false)
    //     //     return;
    //     //
    //     // foreach (var animatorResetter in animResetters)
    //     // {
    //     //     this.Log(animatorResetter.animator, "[PoolObjectResetAndStart] anim Reset", animatorResetter.animator);
    //     //     animatorResetter.ResetToDefault();
    //     //     // this.Break();
    //     // }
    //
    //     // needResetAnim = false;
    // }

    private void CheckList()
    {
        if (inited)
            return;

        // if (IPoolObjectList == null)
        //     IPoolObjectList = new();
        //
        // if (IResetterList == null)
        //     IResetterList = new();

        IPoolObjectList.AddRange(_iPoolObjectRefs);
        // GetComponentsInChildren<IPoolObject>(true, IPoolObjectList);
        IPoolObjectList.Reverse();
        // IPoolObjectList.SortByPriority();
        IResetterList.AddRange(_iResetterRefs);
        // GetComponentsInChildren<IResetter>(true, IResetterList);
        IResetterList.Reverse();
        // IResetterList.SortByPriority();

        inited = true;
    }

    /// <summary>
    /// 重置 Transform 到初始狀態
    /// </summary>
    public void TransformReset()
    {
        if (!CheckResetParameterInit())
            return;

        if (_transformResetOverrider != null)
        {
            _transformResetOverrider.ResetTransform();
        }
        else
        {
            TransformResetHelper.ResetTransform(transform, _resetData);
        }
    }

    /// <summary>
    /// 設置 Transform 並記錄為重置狀態
    /// </summary>
    public void OverrideTransformSetting(
        Vector3 position = default,
        Quaternion rotation = default,
        Transform parentTransform = null,
        Vector3 scale = default
    )
    {
        _resetData = TransformResetHelper.SetupTransform(
            transform,
            position,
            rotation,
            scale,
            parentTransform
        );
        _isResetDataInitialized = true;
    }

    [PreviewInInspector]
    private TransformResetHelper.TransformData _resetData;
    private bool _isResetDataInitialized = false;

    public Vector3 ResetPos => _resetData.position;

    private bool CheckResetParameterInit()
    {
        if (_isResetDataInitialized)
            return true;

        _resetData = TransformResetHelper.CaptureTransformData(transform);
        _isResetDataInitialized = true;
        return false;
    }

    public void OnBorrowFromPool(PoolManager manager)
    {
        onScene = true;
        // EnterLevelResetAndStart();
    }

    /// <summary>
    /// 設定物件為受保護狀態，不會被強制回收
    /// </summary>
    public void MarkAsProtected()
    {
        CurrentProtectionState = ProtectionState.Protected;
    }

    /// <summary>
    /// 設定物件為可回收狀態，允許被池管理器回收
    /// </summary>
    public void MarkAsRecyclable()
    {
        CurrentProtectionState = ProtectionState.Recyclable;
    }

    /// <summary>
    /// 檢查物件是否被保護
    /// </summary>
    public bool IsProtected()
    {
        return CurrentProtectionState == ProtectionState.Protected;
    }

    /// <summary>
    /// 檢查物件是否可以回收
    /// </summary>
    public bool IsRecyclable()
    {
        return CurrentProtectionState == ProtectionState.Recyclable;
    }

    // IPoolableObject interface implementation
    /// <summary>
    /// 設置保護狀態（IPoolableObject介面實作）
    /// </summary>
    public void SetProtectionState(bool isProtected)
    {
        if (isProtected)
        {
            MarkAsProtected();
        }
        else
        {
            MarkAsRecyclable();
        }
    }

    public void PoolObjectResetAndStart() //只有收進去pool的才需要這個
    {
        // this.Break();
        CheckList();
        // ResetAnim();
        PoolLogger.LogInfo("PoolObjectResetAndStart executed", this);
        PoolManager.ResetReload(gameObject);

        foreach (var iBorrowOnEnable in IPoolBorrowedList)
        {
            iBorrowOnEnable.OnBorrowFromPoolOnEnable();
        }

        // 啟動延遲自動回收
        StartAutoRecycleIfEnabled();
    }

    private void StartAutoRecycleIfEnabled()
    {
        if (!_enableAutoRecycle)
            return;

        CancelAutoRecycle();
        _autoRecycleCts = new CancellationTokenSource();
        AutoRecycleAsync(_autoRecycleCts.Token).Forget();
    }

    private async UniTaskVoid AutoRecycleAsync(CancellationToken ct)
    {
        _autoRecycleRemainingTime = _autoRecycleDelay;

        while (_autoRecycleRemainingTime > 0f)
        {
            if (ct.IsCancellationRequested)
                return;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            _autoRecycleRemainingTime -= Time.deltaTime;
        }

        if (!ct.IsCancellationRequested && this != null && isOnScene)
        {
            PoolLogger.LogInfo($"Auto recycle triggered after {_autoRecycleDelay}s", this);
            Recycle();
        }
    }

    private void CancelAutoRecycle()
    {
        if (_autoRecycleCts != null)
        {
            _autoRecycleCts.Cancel();
            _autoRecycleCts.Dispose();
            _autoRecycleCts = null;
        }
        _autoRecycleRemainingTime = 0f;
    }

    /// <summary>
    /// 手動延遲回收（可從外部呼叫，會覆蓋自動回收設定）
    /// </summary>
    public void RecycleDelayed(float delaySeconds)
    {
        CancelAutoRecycle();
        _autoRecycleCts = new CancellationTokenSource();
        RecycleDelayedAsync(delaySeconds, _autoRecycleCts.Token).Forget();
    }

    private async UniTaskVoid RecycleDelayedAsync(float delaySeconds, CancellationToken ct)
    {
        _autoRecycleRemainingTime = delaySeconds;

        while (_autoRecycleRemainingTime > 0f)
        {
            if (ct.IsCancellationRequested)
                return;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            _autoRecycleRemainingTime -= Time.deltaTime;
        }

        if (!ct.IsCancellationRequested && this != null && isOnScene)
        {
            Recycle();
        }
    }

    public void BeforeObjectReturnToPool(PoolManager manager)
    {
        CheckList();
        // ResetAnim();

        foreach (var t in IPoolObjectList)
        {
            try
            {
                t.PoolBeforeReturnToPool();
            }
            catch (Exception e)
            {
                PoolLogger.LogError(
                    $"Error in PoolBeforeReturnToPool for {t.GetType().Name}",
                    e,
                    this
                );
            }
        }
    }

    public void OnReturnToPool(PoolManager manager)
    {
        CancelAutoRecycle();
        lastPlayer = null;
        _resetData = TransformResetHelper.TransformData.Create(
            _resetData.position,
            _resetData.rotation,
            _resetData.scale,
            null
        );

        CheckList();

        if (TryGetComponent<PositionConstraint>(out var constraint))
        {
            constraint.enabled = false;
        }

        foreach (var iClearReference in _iClearReferenceRefs)
        {
            iClearReference.ClearReference();
        }

        for (var i = 0; i < IPoolObjectList.Count; i++)
        {
            try
            {
                if (IPoolObjectList[i] == null)
                {
                    PoolLogger.LogError($"IPoolObjectList[{i}] == null", this.gameObject);
                }
                else
                {
                    IPoolObjectList[i].PoolOnReturnToPool();
                }
            }
            catch (Exception e)
            {
                PoolLogger.LogComponentError(IPoolObjectList[i], "PoolOnReturnToPool", e);
            }
        }

        if (OnReturnEvent != null)
        {
            OnReturnEvent.Invoke(this);
            OnReturnEvent.RemoveAllListeners();
        }
    }

    public void Recycle()
    {
        CancelAutoRecycle();
        PoolLogger.LogInfo("Attempting to return to pool", this);
        if (_bindingPoolManager == null)
        {
            PoolLogger.LogWarning("Return to pool failed - no binding pool manager", this);
            gameObject.SetActive(false);
            // GameObject.Destroy(gameObject);
        }
        else
        {
            if (!onScene)
            {
                // Object already returned to pool, prevent double return
                return;
            }

            onScene = false;
            if (OnReturnEvent != null)
            {
                OnReturnEvent.Invoke(this);
                OnReturnEvent.RemoveAllListeners();
            }

            PoolLogger.LogInfo("Successfully returned to pool", this);
            if (TryGetComponent<MonoObj>(out var monoObj))
            {
                monoObj.Despawn();
            }
            else
            {
                //舊規暫時？不該進來
                PoolLogger.LogError("MonoObj component not found on PoolObject", this);
                _bindingPoolManager.ReturnToPool(this);
            }

            // GetComponent<MonoObj>()
            //
        }
    }

    [ShowInInspector]
    public bool IsFromPool => _bindingPoolManager != null;

    [AutoChildren(false)]
    private Animator[] _anims;

    [ReadOnly]
    [ShowInInspector]
    public Animator[] animators => _anims;
    int animDefaultNameHash;

    public void OnPrepare() //還關著的時候
    {
        InitAnimResetters();
        CheckList();
        foreach (var poolObj in IPoolObjectList)
        {
            try
            {
                poolObj.PoolOnPrepared(this);
            }
            catch (Exception e)
            {
                PoolLogger.LogComponentError(poolObj, "PoolOnPrepared", e);
            }
        }
    }

    public bool isOnScene => onScene;

    public bool isInPool => !onScene;

    private bool onScene = false;

    //一開始就在場景上的物件
    public bool UseSceneAsPool => this.gameObject.scene.name != null && OriginalPrefab == null;
    private Transform oriParent; //在場景上的物件，要回到原本的parent

    private void OnDestroy()
    {
        CancelAutoRecycle();

        // RaisePoolObjectReturnEvent();
        //被別人越權刪除前 跟pool講一聲
        if (this.IsFromPool)
        {
            _bindingPoolManager.PoolObjectDestroyed(this);
        }

        OnReturnEvent?.RemoveAllListeners();
        OnReturnEvent = null;
        if (IPoolObjectList != null)
        {
            IPoolObjectList.Clear();
            IPoolObjectList = null;
        }

        if (IResetterList != null)
        {
            IResetterList.Clear();
            IResetterList = null;
        }
    }

    //  public bool Log= false;
    public void EnterSceneAwake()
    {

        //可能可以拔掉
        //收斂情境：hitData不需要跟著
        if (InitPosType == ShootFrom.HitData)
        {
            if (TryGetComponent<PositionConstraint>(out var constraint))
            {
                Destroy(constraint);
                // Debug.LogError("Destroy constraint!", this);
            }
        }

        CheckList();
        //這個要開著才能初始化
        //InitAnimResetters();
        CheckResetParameterInit();
    }

    private void OnValidate()
    {
        // if (InitPosType == ShootFrom.HitData)
        // {
        //     if (TryGetComponent<PositionConstraint>(out var constraint))
        //     {
        //         DestroyImmediate(constraint);
        //     }
        // }
        // else
        // {
        //     var constraint = this.TryGetCompOrAdd<PositionConstraint>();
        // }
    }

    // [Button]
    // public void ResetStateRestore()
    // {
    //     //  Debug.Log("LevelReset", this);
    //     TransformReset();
    //
    //     // ResetAnim();
    //     // this.Break();
    // }

    [Auto(false)]
    private TransformResetOverrider _transformResetOverrider;

    // public void OnBeforePrefabSave()
    // {
    //     //會有nested? 有可能...? 不該？
    //     _monoReferenceCache.RootObj = gameObject;
    //     _monoReferenceCache.StoreReferenceCache();
    // }


    private bool _isAwakeActive = true;
}

public interface TransformResetOverrider
{
    public void ResetTransform();
}

public class PoolObjEvent : UnityEvent<PoolObject> { }
