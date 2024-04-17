using RCGMaker.Core.Attributes;
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

        [PreviewInInspector] [AutoChildren] private AbstractStateAction[] StateActions;

        public UnityEvent OnReturnToPool = new();
    }
}