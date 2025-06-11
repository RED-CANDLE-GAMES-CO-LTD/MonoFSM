using System;
using MonoFSM.Core.Runtime;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.LifeCycle
{
    //重寫FXPlayer
    public class SpawnAction : AbstractStateAction
    {
        //FIXME: 還是限定型別ㄅ
        [FormerlySerializedAs("target")] public MonoPoolObj _target;
     

        protected override void OnStateEnterImplement()
        {
            Debug.Log("InstantiateAction", this);
            //FIXME: 時機點？FixedUpdateNetwork?

            Spawn(_target, transform.position, transform.rotation);
        }


        private MonoPoolObj Spawn(MonoPoolObj obj, Vector3 position, Quaternion rotation)
        {
            var monoObj = GetComponentInParent<MonoPoolObj>();
            return monoObj.WorldUpdateSimulator.Spawn(obj, position, rotation); //Runner.spawn?
            // if (_spawnProcessor != null)
            //     return _spawnProcessor.Spawn(obj, position, rotation);
            //FIXME: singleton是錯的！
            //內建的方法
            // return PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);
        }

        // private void OnEnable()
        // {
        //     OnStateEnterImplement();
        // }
        public override void EventReceived(IEffectHitData arg)
        {
            // base.EventReceived(arg);
            //噴Receiver的位置?
            var t = arg.Receiver.transform;
            Spawn(_target, t.position, t.rotation);
            // var newObj = PoolManager.Instance.BorrowOrInstantiate(target, t.position, t.rotation);
        }
    }

    public interface ISpawnProcessor //想找一個static的對象來生成物件 (但不能真的static，multi peer的話)
    {
        MonoPoolObj Spawn(MonoPoolObj obj, Vector3 position, Quaternion rotation);
    }
}