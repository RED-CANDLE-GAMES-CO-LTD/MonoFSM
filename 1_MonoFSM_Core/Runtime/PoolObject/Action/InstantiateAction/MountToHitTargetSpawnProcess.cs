using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// Spawn 後將物件的 ViewRoot mount 到 hitData 的目標 Entity ViewRoot 上
    /// </summary>
    public class MountToHitTargetSpawnProcess : MonoBehaviour, IAfterSpawnProcess
    {
        public void AfterSpawn(
            MonoObj obj,
            Vector3 position,
            Quaternion rotation,
            GeneralEffectHitData hitData
        )
        {
            if (hitData == null) return;

            var targetEntity = hitData.GeneralReceiver?.BindEntity;
            if (targetEntity == null)
            {
                Debug.LogWarning("[MountToHitTarget] hitData has no target entity", this);
                return;
            }

            if (targetEntity.ViewRoot == null)
            {
                Debug.LogWarning($"[MountToHitTarget] Target {targetEntity.name} has no ViewRoot",
                    this);
                return;
            }

            var spawnedEntity = obj.Entity;
            if (spawnedEntity == null)
            {
                Debug.LogWarning("[MountToHitTarget] Spawned object has no MonoEntity", this);
                return;
            }

            spawnedEntity.ViewRoot.SetFollowTarget(targetEntity.ViewRoot, position,
                rotation);
            Debug.Log(
                $"[MountToHitTarget] Mounted {spawnedEntity.name} to {targetEntity.name} ViewRoot at {position}",
                this);
        }
    }
}
