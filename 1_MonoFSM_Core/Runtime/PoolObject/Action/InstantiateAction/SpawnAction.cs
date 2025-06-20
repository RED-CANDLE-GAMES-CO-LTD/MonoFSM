using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.LifeCycle
{
    //重寫FXPlayer
    public class SpawnAction : AbstractStateAction, IMonoObjectProvider
    {
        [FormerlySerializedAs("target")] public MonoPoolObj _target;

        [CompRef] [AutoChildren] private SpawnEventHandler _spawnEventHandler;

//FIXME: preview scale & rotation
        protected override void OnStateEnterImplement()
        {
            Debug.Log("SpawnAction OnStateEnterImplement", this);
            //FIXME: 時機點？FixedUpdateNetwork?

            Spawn(_target, transform.position, transform.rotation);

            //on spawn要怎麼吃action?
            
        }

        [PreviewInInspector] private MonoPoolObj _lastSpawnedObj;

        private void Spawn(MonoPoolObj obj, Vector3 position, Quaternion rotation)
        {
            var monoObj = GetComponentInParent<MonoPoolObj>();
            var newObj = monoObj.WorldUpdateSimulator.Spawn(obj, position, rotation); //Runner.spawn?
            //用目前這個action的transform的scale,fixme; 可能需要別種？物件本身的scale?還是應該避免
            newObj.transform.localScale = transform.lossyScale;
            _lastSpawnedObj = newObj;
            _spawnEventHandler?.OnSpawn(newObj, position, rotation);
        }

        // private void OnEnable()
        // {
        //     OnStateEnterImplement();
        // }


        public override void ArgEventReceived(IEffectHitData arg)
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
                rotation = Quaternion.LookRotation(arg.hitNormal.Value, receiverTrans.up);
                Debug.Log("hitNormal is not null, using it for rotation" + rotation, this);
            }

            Spawn(_target, pos, rotation);
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
            if (!Application.isPlaying) return _target;
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