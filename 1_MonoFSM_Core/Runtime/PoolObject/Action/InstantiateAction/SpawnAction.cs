using System;
using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.InstantiateAction
{
    //重寫FXPlayer
    public class SpawnAction : AbstractStateAction
    {
        [FormerlySerializedAs("target")] public PoolObject _target;
        [CompRef] [Auto] private ISpawnProcessor _spawnProcessor;

        protected override void OnStateEnterImplement()
        {
            Debug.Log("InstantiateAction", this);
            //FIXME: 時機點？FixedUpdateNetwork?
            Spawn(_target.gameObject, transform.position, transform.rotation);
        }

        private GameObject Spawn(GameObject obj, Vector3 position, Quaternion rotation)
        {
            if (_spawnProcessor != null)
                return _spawnProcessor.Spawn(obj, position, rotation);

            //內建的方法
            return PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);
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
            Spawn(_target.gameObject, t.position, t.rotation);
            // var newObj = PoolManager.Instance.BorrowOrInstantiate(target, t.position, t.rotation);
        }
    }

    public interface ISpawnProcessor
    {
        GameObject Spawn(GameObject obj, Vector3 position, Quaternion rotation);
    }
}