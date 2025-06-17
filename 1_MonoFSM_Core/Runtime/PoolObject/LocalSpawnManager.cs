using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Runtime
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

        public void Despawn(MonoPoolObj obj)
        {
            if (obj == null) return;
            // Unregister the object from the world update simulator
            _worldUpdateSimulator.UnregisterMonoObject(obj);
            // Return the object to the pool
            PoolManager.Instance.ReturnToPool(obj);
        }
    }
}