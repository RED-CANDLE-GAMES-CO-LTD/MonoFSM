using _1_MonoFSM_Core.Runtime.LifeCycle.Update.Simulate;
using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace RCGMaker.Runtime
{
    public class LocalSpawnManager : MonoBehaviour, ISpawnProcessor
    {
        [Auto] private WorldUpdateSimulator _worldUpdateSimulator;
        public GameObject Spawn(GameObject obj, Vector3 position, Quaternion rotation)
        {
            //FIXME: 還要做updateSimulator的註冊？
            return PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);
        }

        public MonoPoolObj Spawn(MonoPoolObj obj, Vector3 position, Quaternion rotation)
        {
            //FIXME: 還要做updateSimulator的註冊？
            var newObj = PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);

            _worldUpdateSimulator.RegisterMonoObject(obj);
            return newObj;
        }
    }
}