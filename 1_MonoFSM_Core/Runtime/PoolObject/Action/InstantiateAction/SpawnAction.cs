using System;
using MonoFSM_Core.Runtime;
using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Runtime.FSM.RCGStateMachine.Action.InstantiateAction
{
    //重寫FXPlayer
    public class SpawnAction : AbstractStateAction
    {
        //FIXME: 還是限定型別ㄅ
        [FormerlySerializedAs("target")] public GameObject _target;
     

        protected override void OnStateEnterImplement()
        {
            Debug.Log("InstantiateAction", this);
            //FIXME: 時機點？FixedUpdateNetwork?

            Spawn(_target, transform.position, transform.rotation);
        }

        private GameObject Spawn(GameObject obj, Vector3 position, Quaternion rotation)
        {
            // if (_spawnProcessor != null)
            //     return _spawnProcessor.Spawn(obj, position, rotation);
            return WorldReseter.Spawn(gameObject, _target, transform.position, transform.rotation);
            //FIXME: singleton是錯的！
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