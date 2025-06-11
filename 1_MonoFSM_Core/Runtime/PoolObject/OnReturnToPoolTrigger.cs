using MonoFSM.Core.Runtime.Action;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace RCGMaker.Runtime
{
    //FIXME: 什麼時候會用到？
    public class OnReturnToPoolTrigger : MonoBehaviour, IPoolObject
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

        public void PoolOnPrepared(PoolObject poolObj)
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