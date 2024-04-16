using UnityEngine;
using UnityEngine.Events;

namespace RCGMaker.Runtime.PoolObject
{
    public class OnReturnToPoolAction : MonoBehaviour, IPoolObject
    {
        public void EnterLevelReset()
        {
        }

        public void ExitLevelAndDestroy()
        {
        }

        public void PoolOnDestroy()
        {
        }

        public void PoolOnPrepared(global::PoolObject poolObj)
        {
        }

        public void PoolBeforeDestroy()
        {
            OnReturnToPool.Invoke();
        }


        public UnityEvent OnReturnToPool = new();
    }
}