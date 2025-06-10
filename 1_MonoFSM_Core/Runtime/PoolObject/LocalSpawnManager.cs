using MonoFSM_Core.Runtime;
using RCGMaker.Runtime.FSM.RCGStateMachine.Action.InstantiateAction;
using UnityEngine;

namespace RCGMaker.Runtime
{
    [RequireComponent(typeof(WorldReseter))]
    public class LocalSpawnManager : MonoBehaviour, ISpawnProcessor
    {
        public GameObject Spawn(GameObject obj, Vector3 position, Quaternion rotation)
        {
            //FIXME: 還要做updateSimulator的註冊？
            return PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);
        }
    }
}