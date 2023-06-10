using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

public interface IPoolObject : IResetter
{
    void PoolOnDestroy();
    void PoolOnPrepared(PoolObject poolObj);
    void PoolBeforeDestroy();
}
[Serializable]
public class AnimatorReseter
{
    int animDefaultNameHash;
    public Animator _anim;
    public AnimatorReseter(Animator anim)
    {
        _anim = anim;
        Fetch();
    }

    public void Fetch()
    {
        if (_anim != null && _anim.runtimeAnimatorController != null && _anim.isActiveAndEnabled)
        {
            animDefaultNameHash = _anim.GetCurrentAnimatorStateInfo(0).fullPathHash;

            //關掉Animator，原本會清資料，重打開把當下的值當作新的default，會爛掉
            _anim.keepAnimatorStateOnDisable = true;
            
            
        }
    }

    public void Reset()
    {
        if (_anim != null && _anim.runtimeAnimatorController != null && _anim.enabled && _anim.isActiveAndEnabled)
        {
            _anim.Play(animDefaultNameHash, 0, 0);
            _anim.Update(0);
        }
    }
}

public interface IPoolObjectPlayer
{
}
public class PoolObject : MonoBehaviour//, IResetter
{
    [ShowInPlayMode] public IPoolObjectPlayer lastPlayer;
    [Button("Find fx to assign")]
    void Find()
    {
        this.SetFilterForAssignPrefab();
    }
    public bool canBePlayByFXplayer = true;//可不可以被Fxplayer 丟出來 (FIXME: 狀聲詞ㄑ)
    public bool IsGlobalPool;
    public enum ShootFrom
    {
        HitData = 0,
        FxPlayer = 1,
    }
    public ShootFrom InitPosType = ShootFrom.HitData;//TODO: 應該是IPoolObject... PoolOnShoot, OnSpawn
    // public bool IsShootFromHitData = true;
    // public List<EffectPositionConstrain> posContraints;
    [HideInInspector]
    public bool busy = false;

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


    [HideInInspector]
    public PoolObject OriginalPrefab;
    // private bool _onUse = false;

    [HideInInspector]
    public PoolManager _bindingPoolManager;

    public PoolObjEvent OnReturnEvent = new PoolObjEvent();

    // public bool IsFromPool()
    // {
    //     return _bindingPoolManager != null;
    // }

    public void SetBindingPool(PoolManager manager)
    {

        _bindingPoolManager = manager;
    }
    List<IPoolObject> IPoolObjectList = new List<IPoolObject>();
    List<IResetter> IResetterList = new List<IResetter>();

    private bool inited = false;
    private void Start()
    {
        CheckList();
        InitAnimResetters();
    }

    private List<AnimatorReseter> animResetters;

    private bool animResetterInited = false;

    private void InitAnimResetters()
    {
        if (animResetterInited)
            return;

        if (this._anims == null)
        {
            Debug.LogError("Anims == null?",this.gameObject);
            return;
        }

        animResetterInited = true;

        animResetters = new List<AnimatorReseter>();

        if(_anims != null)
            for (var i = 0; i < this._anims.Length; i++)
            {
                animResetters.Add(new AnimatorReseter(_anims[i]));
            }


    }
    // private void OnEnable() //從poolObject拿出來要確定動畫有重置，因為有人很壞，還沒開就被call Reset and Start
    // {
    //     if (needResetAnim == false)
    //         return;
    //     ResetAnim();
    // }
    public void ResetAnim()
    {
        if (animResetterInited == false)
            return;

        if (this.isActiveAndEnabled == false)
            return;

        for (int i = 0; i < animResetters.Count; i++)
        {
            this.Log(animResetters[i]._anim, "[PoolObjecResetAndStart] anim Reset", animResetters[i]._anim);
            animResetters[i].Reset();
        }

        // needResetAnim = false;
    }

    private void CheckList()
    {
        if (inited)
            return;

        if (IPoolObjectList == null)
            IPoolObjectList = new();

        if (IResetterList == null)
            IResetterList = new();

        GetComponentsInChildren<IPoolObject>(true, IPoolObjectList);
        IPoolObjectList.Reverse();
        // IPoolObjectList.SortByPriority();
        GetComponentsInChildren<IResetter>(true, IResetterList);
        IResetterList.Reverse();
        // IResetterList.SortByPriority();

        inited = true;
    }




    //Position , Parent, Rotation
    public void TransformReset()
    {
        if (ChickResetParametterInit())
        {
            this.transform.position = initPosition;
            this.transform.rotation = initRotation;
            this.transform.SetParent(initParent);
            this.transform.localScale = initlocalScale;
        }
    }

    public void OverrideTransformSetting(Vector3 p = default(Vector3), Quaternion q = default(Quaternion), Transform t = null, Vector3 scale = default(Vector3))
    {
        initPosition = p;
        initRotation = q;
        initParent = t;
        initlocalScale = scale;
        isResetParametterInit = true;


    }

    // public Vector3 InitPosition => initPosition; 
    private Vector3 initPosition;
    private Quaternion initRotation;
    private Transform initParent;
    private Vector3 initlocalScale;

    public Vector3 ResetPos => initPosition;

    private bool isResetParametterInit = false;
    private bool ChickResetParametterInit()
    {
        if (isResetParametterInit)
            return true;

        initPosition = this.transform.position;
        initRotation = this.transform.rotation;
        initParent = this.transform.parent;
        initlocalScale = this.transform.localScale;

        isResetParametterInit = true;

        return false;
    }



    public void OnBorrowFromPool(PoolManager manager)
    {
        onScene = true;
        if (UseAutoDestroy)
            autoDestroyTimer = AutoDestroyTime;

        // EnterLevelResetAndStart();
    }

    public void OverrideDestroyTime(float time)
    {
        UseAutoDestroy = true;

        AutoDestroyTime = time;
        autoDestroyTimer = AutoDestroyTime;
    }
    public void PoolObjecResetAndStart() //只有收進去pool的才需要這個
    {
        this.Break();
        CheckList();


        //FIXME: animator可能還關著喔
        //因為關monsterCore，才發生這件悲劇
        this.ResetAnim();

        this.Log("[PoolObjecResetAndStart]", this.gameObject);

        for (var i = 0; i < IResetterList.Count; i++)
        {
            try
            {
                // Debug.Log("Resetting:" + IPoolObjectList[i]);

                IResetterList[i].EnterLevelResetAndStart();
            }
            catch (Exception e)
            {
                Debug.LogError("gameObject:" + gameObject + " reset failed");
                Debug.LogError(e.Message, gameObject);
                Debug.LogError(e.StackTrace, gameObject);
            }
        }

    }



    private void EnterLevelResetAndStart()
    {
        for (var i = 0; i < IResetterList.Count; i++)
        {
            try
            {
                IResetterList[i].EnterLevelResetAndStart();
            }
            catch (Exception e)
            {
                Debug.LogError(e.StackTrace, gameObject);
            }
        }
    }

    public void BeforeObjectReturnToPool(PoolManager manager)
    {
        CheckList();

        for (var i = 0; i < IPoolObjectList.Count; i++)
        {
            try
            {
                IPoolObjectList[i].PoolBeforeDestroy();
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
        CheckList();
        // needResetAnim = true;
        this.ResetAnim();
        if (this.TryGetComponent<PositionConstraint>(out var constraint))
        {
            constraint.enabled = false;
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
                    IPoolObjectList[i].PoolOnDestroy();
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
            OnReturnEvent.RemoveAllListeners();
        }
    }

    public void ReturnToPool()
    {

        if (_bindingPoolManager == null)
        {
            this.Log("[PoolObject] return object to pool failed", this.name);
            gameObject.SetActive(false);

            if (OnReturnEvent != null)
            {
                OnReturnEvent.Invoke(this);
                OnReturnEvent.RemoveAllListeners();
            }
            // GameObject.Destroy(gameObject);
        }
        else
        {
            if (!onScene)
            {
                //FIXME: 好像還有return twice問題
                Debug.LogWarning("return object to pool twice!", gameObject);
                return;
            }



            onScene = false;
            _bindingPoolManager.ReturnToPool(this);
        }


    }



    public bool IsFromPool => _bindingPoolManager != null;

    [AutoChildren(false)] private Animator[] _anims;
    [ReadOnly]
    [ShowInInspector]
    public Animator[] animators => _anims;
    int animDefaultNameHash;
    public void OnPrepare() //關的時候
    {
        CheckList();
        AutoAttributeManager.AutoReferenceAllChildren(gameObject);

        for (var i = 0; i < IPoolObjectList.Count; i++)
        {
            try
            {
                IPoolObjectList[i].PoolOnPrepared(this);
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

    public void Update()
    {
        if (UseAutoDestroy)
        {
            autoDestroyTimer -= Time.deltaTime;
            if (autoDestroyTimer <= 0)
            {
                this.ReturnToPool();
            }
        }
    }

    //一開始就在場景上的物件
    public bool UseSceneAsPool => this.gameObject.scene.name != null && OriginalPrefab == null;

    public bool UseAutoDestroy = false;
    public float AutoDestroyTime = 0;
    private float autoDestroyTimer = 0;


    private void OnDestroy()
    {
        //被別人越權刪除前 跟pool講一聲
        if (this.IsFromPool)
        {
            _bindingPoolManager.PoolDictionary[this.OriginalPrefab].PoolObjectOnDestroySignal(this);
        }
    }
}

public class PoolObjEvent : UnityEvent<PoolObject> { }