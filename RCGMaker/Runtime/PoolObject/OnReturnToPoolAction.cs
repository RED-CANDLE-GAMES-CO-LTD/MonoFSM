using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace RCGMaker.Runtime
{
    public class OnReturnToPoolAction : MonoBehaviour, IPoolObject
    {
        public void EnterLevelReset()
        {
        }

        public void ExitLevelAndDestroy()
        {
        }

        public void PoolOnReturnToPool()
        {
        }

        public void PoolOnPrepared(global::PoolObject poolObj)
        {
        }

        public void PoolBeforeReturnToPool()
        {
            OnReturnToPool.Invoke();
        }

        [PreviewInInspector] [AutoChildren] private AbstractStateAction[] StateActions;

        public UnityEvent OnReturnToPool = new();
    }
}