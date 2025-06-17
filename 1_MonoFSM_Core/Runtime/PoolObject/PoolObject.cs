using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MonoFSMCore.Runtime.LifeCycle;
using PrimeTween;
using MonoFSM.Core.Attributes;
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

public interface IFXPlayerOwner
{
    bool IsActive { get; }
}

public interface IPoolObjectPlayer
{
    IFXPlayerOwner Owner { get; }
}

[DisallowMultipleComponent]
public class PoolObject : MonoBehaviour, ISceneAwake, IResetStateRestore
{
    // public MonoReferenceCache _monoReferenceCache; //要是prefab asset才需要

    [BoxGroup("誰噴的")] [ShowInPlayMode] public IPoolObjectPlayer lastPlayer;
#if UNITY_EDITOR
    [ShowInPlayMode] [NonSerialized] public string _lastPlayerName;
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

    public bool canBePlayByFXplayer = true; //可不可以被Fxplayer 丟出來 (FIXME: 狀聲詞ㄑ)
    public bool IsGlobalPool;

    public enum ShootFrom
    {
        HitData = 0,
        FxPlayer = 1,
        HitDataReceiverCenter = 2
    }

    private void OnDisable()
    {
        this.Log("OnDisable");
    }


    //FIXME: 不一定會有hitData呀，怪物被噴出來了
    [Header("決定要跟fxplayer, 還是hitData(Receiver)的位置")]
    public ShootFrom InitPosType = ShootFrom.HitData; //TODO: 應該是IPoolObject... PoolOnShoot, OnSpawn
    // public bool IsShootFromHitData = true;
    // public List<EffectPositionConstrain> posContraints;
    // [HideInInspector]
    // public bool busy = false;

    // public int UnsolvedIssueBeforeDestroy
    // {
    //     get
    //     {
    //         return _unsolvedIssueBeforeDestroy;
    //     }
    //     set
    //     {
    //         _unsolvedIssueBeforeDestroy = value;
    //     }
    // }

    // private int _unsolvedIssueBeforeDestroy = 0;


    [HideInInspector] public PoolObject OriginalPrefab;
    // private bool _onUse = false;

    [HideInInspector] public PoolManager _bindingPoolManager;

    public PoolObjEvent OnReturnEvent = new PoolObjEvent();

    // public bool IsFromPool()
    // {
    //     return _bindingPoolManager != null;
    // }

    public void SetBindingPool(PoolManager manager)
    {
        _bindingPoolManager = manager;
    }

    private List<IPoolObject> IPoolObjectList = new();
    private List<IResetter> IResetterList = new();

    [AutoChildren] private IClearReference[] _iClearReferenceRefs;
    [AutoChildren] private IPoolObject[] _iPoolObjectRefs;
    [AutoChildren] private IResetter[] _iResetterRefs;

    [PreviewInInspector] [AutoChildren] private IPoolBorrowOnEnable[] IPoolBorrowedList;
    private bool inited = false;

    [PreviewInInspector] private List<AnimatorResetter> animResetters = new();


    [PreviewInInspector] private bool _animResetterInited = false;

    private void InitAnimResetters() //一次就夠了, FIXME: defensive爛扣一個進入點的話就沒有這個問題??
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
        //FIXME: 沒有cache, 很爛
        // AutoAttributeManager.AutoReferenceAllChildren(gameObject);
        // PoolManager.PreparePoolObjectImplementation(this);
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


    //Position , Parent, Rotation
    public void TransformReset()
    {
        if (!CheckResetParameterInit()) return; //FIXME: 這什麼意思？ 還沒初始化過，就塞回去會錯
        if (_transformResetOverrider != null)
        {
            _transformResetOverrider.ResetTransform();
        }
        else
        {
            var transform1 = transform;
            transform1.SetParent(initParent);
            //rigidbody2d的位置還沒跟上？
            transform1.localPosition = initPosition;
            //在levelreset的時候有call這個應該就對了，讓物理跟上transform
            // Physics2D.SyncTransforms();
            // Debug.Log("[PoolObjectResetAndStart] transform Reset", gameObject);
            transform1.localRotation = initRotation;

            transform1.localScale = initlocalScale;
        }
    }

    public void OverrideTransformSetting(Vector3 p = default, Quaternion rotation = default,
        Transform parentTransform = null, Vector3 scale = default)
    {
        var transform1 = transform;

        transform1.SetParent(parentTransform);
        transform1.position = p;
        transform1.rotation = rotation;
        // Debug.Log("[PoolObjectResetAndStart] transform" + transform1.rotation, transform1.parent);
        initPosition = transform1.localPosition;
        initRotation = transform1.localRotation;
        //FIXME: 為什麼這個把initParent改掉了?
        initParent = parentTransform;
        // Debug.Log("[PoolObjectResetAndStart] transform initParent", t);
        initlocalScale = scale;
        isResetParameterInit = true;
    }

    // public Vector3 InitPosition => initPosition; 
    private Vector3 initPosition;

    public void OverrideInitPosition(Vector3 pos)
    {
        initPosition = pos;
        var transform1 = transform;
        initRotation = transform1.localRotation;
        // Debug.Log("[PoolObjectResetAndStart] transform initParent", transform1.parent);
        initParent = transform1.parent;
        initlocalScale = transform1.localScale;
        isResetParameterInit = true;
    }

    [PreviewInInspector] private Quaternion initRotation;

    [PreviewInInspector] private Vector3 InitEulerRotation => initRotation.eulerAngles;

    [ShowInPlayMode] private Transform initParent;
    private Vector3 initlocalScale;

    public Vector3 ResetPos => initPosition;

    private bool isResetParameterInit = false;

    private bool CheckResetParameterInit()
    {
        if (isResetParameterInit)
            return true;

        var transform1 = transform;
        initPosition = transform1.localPosition;
        initRotation = transform1.localRotation;
        initParent = transform1.parent;
        // Debug.Log("[PoolObjectResetAndStart] transform initParent", transform1.parent);
        initlocalScale = transform.localScale;
        isResetParameterInit = true;

        return false;
    }


    public void OnBorrowFromPool(PoolManager manager)
    {
        onScene = true;

        // EnterLevelResetAndStart();
    }

    //Shooter 想要變更Destory 設定
    public void OverrideDestroyTime(float time)
    {
        // RaisePoolObjectReturnEvent();

        UseAutoDestroy = true;
        AutoDestroyTime = time;

        RegisterDestroy();
    }

    public void PoolObjectResetAndStart() //只有收進去pool的才需要這個
    {
        // this.Break();
        CheckList();
        // ResetAnim();
        this.Log("[PoolObjectResetAndStart]", gameObject);
        PoolManager.ResetReload(gameObject);

        foreach (var iBorrowOnEnable in IPoolBorrowedList)
        {
            iBorrowOnEnable.OnBorrowFromPoolOnEnable();
        }

        if (UseAutoDestroy)
        {
            RegisterDestroy(); //打開了才註冊ㄋ
        }
    }


    public void BeforeObjectReturnToPool(PoolManager manager)
    {
        destroyTween.Stop();
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
                Debug.LogError(e.Message);
            }
        }
    }

    // private bool needResetAnim = false;

    public void OnReturnToPool(PoolManager manager)
    {
        lastPlayer = null;
        initParent = null;
        // Debug.Log("[PoolObject] return to pool", this);
        // RaisePoolObjectReturnEvent();
        CheckList();
        // needResetAnim = true;

        destroyTween.Stop();
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
                    Debug.LogError("IPoolObjectList[" + i + "] == null", this.gameObject);
                }
                else
                {
                    IPoolObjectList[i].PoolOnReturnToPool();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.StackTrace);
            }
        }

        if (OnReturnEvent != null)
        {
            OnReturnEvent.Invoke(this);
            OnReturnEvent.RemoveAllListeners(); //FIXME: 這個會GC!
        }
    }

    public void ReturnToPool()
    {
        destroyTween.Stop();
        this.Log("[PoolObject] return 0", gameObject);
        if (_bindingPoolManager == null)
        {
            this.Log("[PoolObject] return object to pool failed", this);
            gameObject.SetActive(false);
            // GameObject.Destroy(gameObject);
        }
        else
        {
            if (!onScene)
            {
                //FIXME: 好像還有return twice問題
                //                Debug.LogWarning("return object to pool twice!", gameObject);
                return;
            }

            onScene = false;
            if (OnReturnEvent != null)
            {
                OnReturnEvent.Invoke(this);
                OnReturnEvent.RemoveAllListeners();
            }

            this.Log("[PoolObject] return object to pool", gameObject);
            // destroyTween.Stop();
            _bindingPoolManager.ReturnToPool(this);
        }
    }


    public bool IsFromPool => _bindingPoolManager != null;

    [AutoChildren(false)] private Animator[] _anims;
    [ReadOnly] [ShowInInspector] public Animator[] animators => _anims;
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
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
            }
        }
    }

    public bool isOnScene => onScene;

    public bool isInPool => !onScene;

    private bool onScene = false;

    private void RegisterDestroy()
    {
        if (UseAutoDestroy)
        {
            Debug.Log("RegisterDestroy" + AutoDestroyTime, this);
            destroyTween.Stop();
            // UniTask.Delay(TimeSpan.FromSeconds(AutoDestroyTime)).Forget();
            destroyTween = this.DelayTask(AutoDestroyTime, (target) =>
            {
                Debug.Log("DelayTask AutoDestroyTime", target);
                target.ReturnToPool();
                target.Log("AutoDestroyTime:", target.AutoDestroyTime);
            });
        }
    }

    [PreviewInInspector] private Tween destroyTween;


    //一開始就在場景上的物件
    public bool UseSceneAsPool => this.gameObject.scene.name != null && OriginalPrefab == null;
    private Transform oriParent; //在場景上的物件，要回到原本的parent

    public bool UseAutoDestroy = false;
    [ShowIf(nameof(UseAutoDestroy))] public float AutoDestroyTime = 0; //fixme: 用-1就好了？

    private void OnDestroy()
    {
        // RaisePoolObjectReturnEvent();
        destroyTween.Stop();
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

    [Button]
    public void ResetStateRestore()
    {
        //  Debug.Log("LevelReset", this);
        TransformReset();

        // ResetAnim();
        destroyTween.Stop();
        // this.Break();
    }

    [Auto(false)] private TransformResetOverrider _transformResetOverrider;


    // public void OnBeforePrefabSave()
    // {
    //     //會有nested? 有可能...? 不該？
    //     _monoReferenceCache.RootObj = gameObject;
    //     _monoReferenceCache.StoreReferenceCache();
    // }
}

public interface TransformResetOverrider
{
    public void ResetTransform();
}

public class PoolObjEvent : UnityEvent<PoolObject>
{
}