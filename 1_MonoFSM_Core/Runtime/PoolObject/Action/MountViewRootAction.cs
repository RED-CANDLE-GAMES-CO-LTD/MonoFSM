using MonoFSM.Core.Runtime.Action;
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
        [SerializeField] private Transform _mountPoint;
        [SerializeField] private bool _handlePhysics = true;

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

            if (_mountPoint == null)
            {
                Debug.LogWarning("[MountViewRoot] MountPoint Transform is null", this);
                return;
            }

            // 處理物理：設 kinematic、清速度、關 colliders
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

            source.ViewRoot.SetFollowTarget(target.ViewRoot, _mountPoint.position,
                _mountPoint.rotation);

            Debug.Log(
                $"[MountViewRoot] Mounted {source.name} to {target.name} at {_mountPoint.position}",
                this);
        }
    }
}
