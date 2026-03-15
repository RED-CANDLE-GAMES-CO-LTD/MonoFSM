using System;
using JetBrains.Annotations;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    //FIXME: 還是走SpawnEventHandler的方式？ 但參數呢？
    //IsServer才能spawn?
    //需要preprocess? 這樣就可以提早決定位置 (不用重複set之類的)
    public interface IAfterSpawnProcess //action在Spawn之後執行 也有種event的概念？AfterSpawnEventHandler?
    {
        public void AfterSpawn(
            MonoObj obj,
            Vector3 position,
            Quaternion rotation,
            [CanBeNull] GeneralEffectHitData hitData
        ); //對著某個東西spawn?
    }

    //重寫FXPlayer
    public class SpawnAction : AbstractArgEventHandler<GeneralEffectHitData>, IMonoObjectProvider //ICompProvider<MonoPoolObj>
    {
        //FIXME: 下面要有各種preProcess action?
        // [Required]
        // [CompRef]
        // [AutoChildren(DepthOneOnly = true)]
        // [ValueTypeValidate(typeof(MonoObj))]
        // private ValueProvider _poolObjProvider; //使用VarPoolObj來存儲目標物件

        // [Required] [SerializeField] private VarEntity _poolObjVar; //用來存取剛spawn的物件
        public override string Description => "Spawn " + (_poolObjFoldOut.Value != null
            ? _poolObjFoldOut.Value?.name
            : _poolObjVar != null
                ? _poolObjVar.Value?.name
                : "null");

        [InlineField]
        [SerializeField]
        VarMonoObjFoldOut _poolObjFoldOut;

        [Obsolete]
        // [Required]
        [SerializeField]
        private VarMonoObj _poolObjVar; //不要用了？轉用_poolObjFoldOut?

        [BoxGroup("Scale")]
        [HideIf(nameof(_scaleRatio))]
        public bool _isUsingSpawnTransformScale;

        [BoxGroup("Scale")]
        [SerializeField]
        VarFloat _scaleRatio;

        //FIXME: Spawn可能有有很多需求，...
        [SerializeField]
        private Transform _spawnPosition; //Position provider, VarVector3

        public VarVector3 _spawnPositionV3; //可以用provider

        Vector3 spawnPos =>
            _spawnPosition != null
                ? _spawnPosition.position
                : (_spawnPositionV3 != null ? _spawnPositionV3.Value : transform.position);

        [SerializeField]
        private bool _isRotationIdentity;

        /// <summary>
        /// tmp local obj
        /// </summary>
        // [Required]
        [Component]
        [SerializeField]
        private VarEntity
            _spawnedEntityVar; //怎麼統一幫這個rename? fetch all var and rename? attribute LocalRuntimeField?

        [CompRef]
        [AutoChildren]
        private IAfterSpawnProcess[] _preSpawnActions;

        //檢查？SerializeClass的話？

        //型別篩選 vs attribute篩選？
        //FIXME: 應該要分ConfigPrefabProvider, 不可以都用ICompProvider，因為會有Runtime的Prefab
        //Prefab特殊，不該runtime去Instantiate對ㄅ，都應該從prefab來，就算要也是複製狀態

        [PreviewInDebugMode]
        private MonoObj Prefab =>
            _poolObjFoldOut.Value != null ? _poolObjFoldOut.Value : _poolObjVar?.Value; // _poolObjProvider?.Get<MonoObj>();

        [CompRef]
        [AutoChildren]
        private SpawnEventHandler _spawnEventHandler;

        //FIXME: preview scale & rotation
        protected override void OnActionExecuteImplement()
        {
            // Debug.Log("SpawnAction OnStateEnterImplement", this);
            //FIXME: 時機點？FixedUpdateNetwork?
            //外包transform?
            // var t = _spawnPosition != null ? _spawnPosition : transform;
            var rotation =
                _isRotationIdentity ? Quaternion.identity
                : _spawnPosition ? _spawnPosition.rotation
                : transform.rotation;
            Spawn(Prefab, spawnPos, rotation, null);
        }

        [PreviewInInspector]
        private MonoObj _lastSpawnedObj;

        // [Required] [SerializeField] private VarMonoObj _spawnedObjVar; //用來存取剛spawn的物件
        //FIXME: 自動判定？好麻煩喔
        [SerializeField]
        public bool _isSpawningVisual; //有NetworkObject的話就不需要?

        //FIXME: 寫死各種參數的介面不好！provdier?
        private void Spawn(
            MonoObj prefab,
            Vector3 position,
            Quaternion rotation,
            [CanBeNull] GeneralEffectHitData hitData
        )
        {
            //FIXME: canspawn?
            if (prefab == null)
            {
                Debug.LogError("SpawnAction: Prefab is null", this);
                return;
            }

            if (_parentObj == null)
            {
                Debug.LogError("SpawnAction: No MonoPoolObj found in parent", this);
                return;
            }

            if (_parentObj.WorldUpdateSimulator == null)
            {
                Debug.LogError(
                    $"SpawnAction:{name}, No WorldUpdateSimulator found in _parentObj",
                    _parentObj
                );
                Debug.Break();
                return;
            }

            MonoObj newObj;
            if (_isSpawningVisual)
                newObj = _parentObj.WorldUpdateSimulator.SpawnVisual(prefab, position, rotation); //Runner.spawn?
            else
                newObj = _parentObj.WorldUpdateSimulator.Spawn(prefab, position, rotation); //Runner.spawn?
            if (newObj == null)
                return;
            //用目前這個action的transform的scale,fixme; 可能需要別種？物件本身的scale?還是應該避免
            //fixme: 為什麼要這樣？
            if (_scaleRatio != null)
            {
                Debug.Log("SpawnAction: Applying scale ratio " + _scaleRatio.CurrentValue, this);
                newObj.transform.localScale = Vector3.one * _scaleRatio.CurrentValue;
            }
            else if (_isUsingSpawnTransformScale)
                newObj.transform.localScale = transform.lossyScale;

            newObj.gameObject.SetActive(true);
            _spawnedEntityVar?.SetValue(newObj.GetComponent<MonoEntity>(), this); //更新變數
            //Rotation呢？
            _lastSpawnedObj = newObj;
            _spawnEventHandler?.OnSpawn(newObj, position, rotation);

            foreach (var preSpawnAction in _preSpawnActions)
                preSpawnAction.AfterSpawn(newObj, position, rotation, hitData);


            newObj.HandleAfterSpawn(position, rotation,
                hitData); //讓spawn出來的物件自己的 IAfterSpawnProcess 也能處理
        }

        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            // base.EventReceived(arg);
            //噴Receiver的位置?
            var receiverTrans = arg._receiverSourceObj.transform;

            if (arg.hitPoint == null)
                Debug.LogError(
                    "SpawnAction received hitData with null hitPoint, using receiver's position instead",
                    this);
            //FIXME: 不要用 receiverTrans.position...
            var pos = arg.hitPoint ?? receiverTrans.position; //如果沒有hitPoint，就用Receiver的位置
            Debug.Log(
                "SpawnAction EventReceived, pos: " + pos + ", hitPoint: " + arg.hitPoint,
                this
            );
#if UNITY_EDITOR //Debug 用
            transform.position = pos;
            // Debug.Break();
#endif

            //FIXME: arg是EffectHitData...point和normal都放過來嗎？
            var rotation = receiverTrans.rotation;
            if (arg.hitNormal != null)
            {
                // up 改為 Vector3.up，避免參考 receiverTrans
                var normal = arg.hitNormal.Value.normalized;
                var up = Vector3.up;
                // 避免 up 跟 normal 太接近導致 LookRotation 不穩定
                if (Mathf.Abs(Vector3.Dot(normal, up)) > 0.99f)
                    up = Vector3.right;
                rotation = Quaternion.LookRotation(normal, up);
                // Debug.Log("has hitNormal, using it for rotation " + rotation, this);
            }

            Spawn(Prefab, pos, rotation, arg);
            // var newObj = PoolManager.Instance.BorrowOrInstantiate(target, t.position, t.rotation);
        }

        public MonoObj GetMonoObject()
        {
            if (_lastSpawnedObj != null)
            {
                return _lastSpawnedObj;
            }
            else
            {
                Debug.LogError("No object spawned yet, returning null", this);
                return null; //或許可以拋出異常？
            }
        }

        public object GetValue()
        {
            if (_lastSpawnedObj != null)
            {
                return _lastSpawnedObj;
            }
            else
            {
                Debug.LogError("No object spawned yet, returning null", this);
                return null; //或許可以拋出異常？
            }
        }

        public Type ValueType => typeof(MonoObj);

        public MonoObj Get()
        {
            if (!Application.isPlaying)
                return Prefab;
            if (_lastSpawnedObj != null)
            {
                return _lastSpawnedObj;
            }
            else
            {
                if (Application.isPlaying)
                    Debug.LogError("No object spawned yet, returning null", this);
                return null; //或許可以拋出異常？
            }
        }
    }

    public interface ISpawnProcessor //想找一個static的對象來生成物件 (但不能真的static，multi peer的話)
    {
        MonoObj Spawn(MonoObj obj, Vector3 position, Quaternion rotation);
        public void Despawn(MonoObj obj);
    }
}
