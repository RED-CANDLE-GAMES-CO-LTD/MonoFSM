using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Simulate;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Runtime
{
    //FIXME: fusion network不做一個對稱的？
    public class LocalSpawnManager : MonoBehaviour, ISpawnProcessor //local spawner應該直接和worldUpdateSimulator整合在一起？
    {
        [Auto]
        private WorldUpdateSimulator _worldUpdateSimulator;

        // public GameObject Spawn(GameObject obj, Vector3 position, Quaternion rotation)
        // {
        //     //FIXME: 還要做updateSimulator的註冊？
        //     return PoolManager.Instance.BorrowOrInstantiate(obj, position, rotation);
        // }

        public MonoObj Spawn(MonoObj obj, Vector3 position, Quaternion rotation)
        {
            //FIXME: 還要做updateSimulator的註冊？
            var newObj = _worldUpdateSimulator.Pool.BorrowOrInstantiate(obj, position, rotation);
            _worldUpdateSimulator.AfterPoolSpawn(newObj);
            return newObj;
        }

        public void Despawn(MonoObj obj)
        {
            if (obj == null)
                return;
            Debug.Log("LocalSpawnManager: Despawning object " + obj, this);
            // Return the object to the pool
            _worldUpdateSimulator.Pool.ReturnToPool(obj);
        }
    }
}
