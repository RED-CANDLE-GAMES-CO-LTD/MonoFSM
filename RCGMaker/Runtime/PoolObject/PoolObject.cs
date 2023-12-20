using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PrimeTween;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Serialization;

public interface IPoolObject : IResetter
{
    void PoolOnDestroy();
    void PoolOnPrepared(PoolObject poolObj);
    void PoolBeforeDestroy();
}


public interface IPoolObjectPlayer
{
}

public class PoolObject : MonoBehaviour, ILevelAwake , ILevelReset
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

    private void OnDisable()
    {
        this.Log("OnDisable");
    }


    //FIXME: 不一定會有hitData呀，怪物被噴出來了
    [Header("決定要跟fxplayer, 還是hitData(Receiver)的位置")]
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
    private List<AnimatorResetter> animResetters;

    private bool animResetterInited = false;

    private void InitAnimResetters()
    {
        if (animResetterInited)
            return;

        if (this._anims == null)
        {
           // Debug.LogError("Anims == null?",this.gameObject);
            return;
        }

        animResetterInited = true;

        animResetters = new List<AnimatorResetter>();

        if(_anims != null)
            for (var i = 0; i < this._anims.Length; i++)
            {
                animResetters.Add(new AnimatorResetter(_anims[i]));
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
            this.Log(animResetters[i].animator, "[PoolObjecResetAndStart] anim Reset", animResetters[i].animator);
            animResetters[i].ResetToDefault();
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
            var transform1 = transform;
            transform1.SetParent(initParent);
            transform1.localPosition = initPosition;
            transform1.localRotation = initRotation;

            transform1.localScale = initlocalScale;
        }
    }

    public void OverrideTransformSetting(Vector3 p = default(Vector3), Quaternion q = default(Quaternion), Transform t = null, Vector3 scale = default(Vector3))
    {
        var transform1 = transform;
        transform1.SetParent(t);
        transform1.position = p;
        transform1.rotation = q;

        initPosition = transform1.localPosition;
        initRotation = transform1.localRotation;
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

        initPosition = transform.localPosition;
        initRotation = transform.localRotation;
        initParent = transform.parent;
        initlocalScale = transform.localScale;

        isResetParametterInit = true;

        return false;
    }



    public void OnBorrowFromPool(PoolManager manager)
    {
       
        
        onScene = true;
        if (UseAutoDestroy)
        {
            //FIXME:
            RegisterDestroy();
            // autoDestroyTimer = AutoDestroyTime;
        }
            

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

                IResetterList[i].EnterLevelReset();
            }
            catch (Exception e)
            {
                Debug.LogError("gameObject:" + gameObject + " reset failed");
                Debug.LogError(e.Message, gameObject);
                Debug.LogError(e.StackTrace, gameObject);
            }
        }

    }

    

    public void BeforeObjectReturnToPool(PoolManager manager)
    {
        // if(Log)
        //     Debug.Log("BeforeObjectReturnToPool:"+this.gameObject.name,this);
        //
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
        // RaisePoolObjectReturnEvent();
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
            OnReturnEvent.RemoveAllListeners(); //FIXME: 這個會GC!
        } 
    }

    public void ReturnToPool()
    {
        destroyTween.Stop();
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

            // Debug.Log("[PoolObject] return object to pool" + name, this);
            // destroyTween.Stop();
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

    private void RegisterDestroy()
    {
        if (UseAutoDestroy)
        {
            _poolObjectReturnTokenSource = new CancellationTokenSource();
            destroyTween = this.DelayTask(AutoDestroyTime, (target) =>
            {
                target.ReturnToPool();
                Debug.Log("AutoDestroyTime:" + target.AutoDestroyTime, target);
            });
            // await this.Delay(AutoDestroyTime,_poolObjectReturnTokenSource.Token);
            // ReturnToPool();
        }
    }

    private Tween destroyTween;

    // private void RaisePoolObjectReturnEvent()
    // {
    //     if (_poolObjectReturnTokenSource != null)
    //     {
    //         _poolObjectReturnTokenSource.Cancel();
    //         _poolObjectReturnTokenSource.Dispose();
    //         _poolObjectReturnTokenSource = null;
    //     }
    // }

    private CancellationTokenSource _poolObjectReturnTokenSource;

    // public void Update()
    // {
    //     if (UseAutoDestroy)
    //     {
    //         autoDestroyTimer -= Time.deltaTime;
    //         if (autoDestroyTimer <= 0)
    //         {
    //             this.ReturnToPool();
    //         }
    //     }
    // }

    //一開始就在場景上的物件
    public bool UseSceneAsPool => this.gameObject.scene.name != null && OriginalPrefab == null;

    public bool UseAutoDestroy = false;
    public float AutoDestroyTime = 0;

    private void OnDestroy()
    {
        // RaisePoolObjectReturnEvent();
        destroyTween.Stop();
        //被別人越權刪除前 跟pool講一聲
        if (this.IsFromPool)
        {
            _bindingPoolManager.PoolObjectDestroyed(this);
        }
    }

  //  public bool Log= false;
  public void EnterLevelAwake()
  {
      //可能可以拔掉
      //收斂情境：hitData不需要跟著
      if (InitPosType == ShootFrom.HitData)
      {
          if (TryGetComponent<PositionConstraint>(out var constraint))
          {
              Destroy(constraint);
              Debug.LogError("Destroy constraint!", this);
          }
      }
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

  public void LevelReset()
  {
      TransformReset();
      ResetAnim();
      destroyTween.Stop();
  }
}

public class PoolObjEvent : UnityEvent<PoolObject> { }