using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable.Attributes;
using MonoFSM.Runtime.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    //重寫FXPlayer
    public class
        SpawnAction : AbstractStateAction<GeneralEffectHitData>, IMonoObjectProvider //ICompProvider<MonoPoolObj>
    {
        //崩潰..Prefab和Runtime混在一起耶，所以拿Var比較好？但實際上不是啊...
        // [CompRef] [AutoChildren(DepthOneOnly = true)]
        // private ICompProvider<MonoPoolObj> _targetVarProvider; //使用VarPoolObj來存儲目標物件

        [Required] [CompRef] [AutoChildren(DepthOneOnly = true)] [ValueTypeValidate(typeof(MonoPoolObj))]
        private DataProvider.ValueProvider _poolObjProvider; //使用VarPoolObj來存儲目標物件

        //檢查？SerializeClass的話？

        //型別篩選 vs attribute篩選？
        //FIXME: 應該要分ConfigPrefabProvider, 不可以都用ICompProvider，因為會有Runtime的Prefab
        //Prefab特殊，不該runtime去Instantiate對ㄅ，都應該從prefab來，就算要也是複製狀態

        private MonoPoolObj Prefab => _poolObjProvider?.Get<MonoPoolObj>();

        [CompRef] [AutoChildren] private SpawnEventHandler _spawnEventHandler;

        //FIXME: preview scale & rotation
        protected override void OnActionExecuteImplement()
        {
            Debug.Log("SpawnAction OnStateEnterImplement", this);
            //FIXME: 時機點？FixedUpdateNetwork?

            Spawn(Prefab, transform.position, transform.rotation);

            //on spawn要怎麼吃action?
            
        }

        [PreviewInInspector] private MonoPoolObj _lastSpawnedObj;

        private void Spawn(MonoPoolObj obj, Vector3 position, Quaternion rotation)
        {
            var monoObj = GetComponentInParent<MonoPoolObj>();
            if (monoObj == null)
            {
                Debug.LogError("SpawnAction: No MonoPoolObj found in parent", this);
                return;
            }

            if (monoObj.WorldUpdateSimulator == null)
            {
                Debug.LogError("SpawnAction: No WorldUpdateSimulator found in MonoPoolObj", monoObj);
                return;
            }
            var newObj = monoObj.WorldUpdateSimulator.Spawn(obj, position, rotation); //Runner.spawn?
            //用目前這個action的transform的scale,fixme; 可能需要別種？物件本身的scale?還是應該避免
            newObj.transform.localScale = transform.lossyScale;
            //Rotation呢？
            _lastSpawnedObj = newObj;
            _spawnEventHandler?.OnSpawn(newObj, position, rotation);
        }

        // private void OnEnable()
        // {
        //     OnStateEnterImplement();
        // }


        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            // base.EventReceived(arg);
            //噴Receiver的位置?
            var receiverTrans = arg.Receiver.transform;

            var pos = arg.hitPoint ?? receiverTrans.position; //如果沒有hitPoint，就用Receiver的位置
            Debug.Log("SpawnAction EventReceived, pos: " + pos + ", hitPoint: " + arg.hitPoint, this);

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
                Debug.Log("hitNormal is not null, using it for rotation " + rotation, this);
            }

            Spawn(Prefab, pos, rotation);
            // var newObj = PoolManager.Instance.BorrowOrInstantiate(target, t.position, t.rotation);
        }

        public MonoPoolObj GetMonoObject()
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

        public Type ValueType => typeof(MonoPoolObj);


        public MonoPoolObj Get()
        {
            if (!Application.isPlaying) return Prefab;
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
        MonoPoolObj Spawn(MonoPoolObj obj, Vector3 position, Quaternion rotation);
        public void Despawn(MonoPoolObj obj);
    }
}