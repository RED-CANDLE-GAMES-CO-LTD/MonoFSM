using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// Spawn 後將物件的 ViewRoot mount 到 hitData 的目標 Entity ViewRoot 上
    /// </summary>
    public class MountToHitTargetSpawnProcess : MonoBehaviour, IAfterSpawnProcess
    {
        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        private bool _isMounted;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        private MonoEntity _targetEntity;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        private MonoEntity _spawnedEntity;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        private Vector3 _mountPosition;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        private Quaternion _mountRotation;

        public void AfterSpawn(
            MonoObj obj,
            Vector3 position,
            Quaternion rotation,
            GeneralEffectHitData hitData
        )
        {
            _isMounted = false;
            _targetEntity = null;
            _spawnedEntity = null;

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

            _isMounted = true;
            _targetEntity = targetEntity;
            _spawnedEntity = spawnedEntity;
            _mountPosition = position;
            _mountRotation = rotation;

            Debug.Log(
                $"[MountToHitTarget] Mounted {spawnedEntity.name} to {targetEntity.name} ViewRoot at {position}",
                this);
        }
    }
}
