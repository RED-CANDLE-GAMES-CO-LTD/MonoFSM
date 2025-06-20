using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    
    //也不用繼承啊...type?
    public class SpawnEventHandler : AbstractEventHandler
    {
        // [CompRef] [AutoChildren] private AbstractStateAction[] _actions;

        public void OnSpawn(MonoPoolObj obj, Vector3 position, Quaternion rotation)
        {
            EventHandle(obj); //不對啊，為什麼要傳Rigidbody下去？
        }
    }
}