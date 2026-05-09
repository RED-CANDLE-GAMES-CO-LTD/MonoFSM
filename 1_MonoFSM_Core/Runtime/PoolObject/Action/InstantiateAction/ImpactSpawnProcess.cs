using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    //依照hitData來更動位置和面向
    //HitData SpawnProcess
    //FIXME: 太亂了
    public class ImpactSpawnProcess : MonoBehaviour, IAfterSpawnProcess
    {
        //事後才做，好像應該spawn前就當作參數才對？
        public void AfterSpawn(
            MonoObj obj,
            Vector3 position,
            Quaternion rotation,
            GeneralEffectHitData hitData
        )
        {
            //有pos了
            obj.transform.position = position;
            if (hitData != null && hitData.hitNormal != null)
            {
                obj.transform.rotation = Quaternion.LookRotation(hitData.hitNormal.Value);
            }
            // Vector3 normal
        }
    }
}
