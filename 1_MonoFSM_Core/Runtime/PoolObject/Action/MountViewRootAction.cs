using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 將 source Entity 的 ViewRoot mount 到 target Entity 的 ViewRoot 上，
    /// 使用指定的 Transform 作為 mount 位置和方向。
    /// 可選擇同時處理物理狀態（設 kinematic、清速度、關 colliders）。
    /// </summary>
    public class MountViewRootAction : AbstractStateAction
    {
        [SerializeField] private VarEntityWrapper _sourceEntity;
        [SerializeField] private VarEntityWrapper _targetEntity;

        //優先用 VarTransform（沒接 _var 時 wrapper 的 tempValue 也可直接拖 Transform）
        [SerializeField] private VarTransformWrapper _mountPointVar;

        //legacy: 舊場景/prefab 序列化的直接引用，_mountPointVar 沒值時 fallback
        [HideIf(nameof(HasMountPointVar))]
        [SerializeField] private Transform _mountPoint;

        //設 kinematic + 清速度
        [SerializeField] private bool _handlePhysics = true;

        //關掉 source 底下所有 collider（撿起後完全沒有物理性質），Unmount 時自動還原
        [SerializeField] private bool _disableColliders;

        private bool HasMountPointVar => _mountPointVar != null && _mountPointVar.HasValue;
        private Transform MountPoint => HasMountPointVar ? _mountPointVar.Value : _mountPoint;

        protected override void OnActionExecuteImplement()
        {
            var source = _sourceEntity.Value;
            var target = _targetEntity.Value;

            if (source == null)
            {
                Debug.LogWarning("[MountViewRoot] Source entity is null", this);
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("[MountViewRoot] Target entity is null", this);
                return;
            }

            if (source.ViewRoot == null)
            {
                Debug.LogWarning($"[MountViewRoot] Source {source.name} has no ViewRoot", this);
                return;
            }

            if (target.ViewRoot == null)
            {
                Debug.LogWarning($"[MountViewRoot] Target {target.name} has no ViewRoot", this);
                return;
            }

            var mountPoint = MountPoint;
            if (mountPoint == null)
            {
                Debug.LogWarning("[MountViewRoot] MountPoint Transform is null", this);
                return;
            }

            // 處理物理：設 kinematic、清速度
            if (_handlePhysics)
            {
                var rb = source.GetCompCache<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // 關 colliders（從 entity root 收集，包含 Root 之外的 collider 節點）
            if (_disableColliders)
                source.ViewRoot.DisableCollidersForMount(source.transform);

            source.ViewRoot.SetFollowTarget(target.ViewRoot, mountPoint.position,
                mountPoint.rotation);

            // Debug.Log(
            //     $"[MountViewRoot] Mounted {source.name} to {target.name} at {_mountPoint.position}",
            //     this);
        }
    }
}
