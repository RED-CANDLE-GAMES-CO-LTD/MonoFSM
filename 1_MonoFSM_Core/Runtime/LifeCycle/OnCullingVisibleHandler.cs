using MonoFSM.Core;
using UnityEngine;

namespace MonoFSM.Runtime.LifeCycle
{
    public abstract class OnCullingVisibleHandler : MonoBehaviour
    {
        public abstract void OnVisible();

        public abstract void OnInvisible();
    }
}