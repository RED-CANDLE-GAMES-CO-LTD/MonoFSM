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
        [ShowInInspector] [AutoParent] MonoEntity _parentEntity;

        [SerializeField] private VarEntityWrapper _sourceEntity;

        [SerializeField] private VarEntityWrapper _targetEntity; //FIXME:自動撈parent的嗎？要隔兩層

        //優先用 VarTransform（沒接 _var 時 wrapper 的 tempValue 也可直接拖 Transform）
        [SerializeField] private VarTransformWrapper _mountPointVar;

        //legacy: 舊場景/prefab 序列化的直接引用，_mountPointVar 沒值時 fallback
        //FIXME: mountPoint一定要放到viewroot下？
        [HideIf(nameof(HasMountPointVar))]
        [SerializeField] private Transform _mountPoint;

        //設 kinematic + 清速度
        [SerializeField] private bool _handlePhysics = true;

        //關掉 source 底下所有 collider（撿起後完全沒有物理性質），Unmount 時自動還原
        [SerializeField] private bool _disableColliders;

        private bool HasMountPointVar => _mountPointVar != null && _mountPointVar.HasValue;
        private Transform MountPoint => HasMountPointVar ? _mountPointVar.Value : _mountPoint;

#if UNITY_EDITOR
        /// <summary>
        /// 給 Editor 側驗證器讀（例如 Fusion 的 NetworkedMountPoint 漏綁檢查）。
        /// 回 false 表示 mount point 由 runtime 的 Var 決定，editor time 無法判斷、不該報錯。
        /// 純粹暴露既有序列化欄位，不引入任何網路依賴。
        /// </summary>
        public bool TryGetEditorMountPoint(out Transform mountPoint)
        {
            mountPoint = null;
            if (_mountPointVar != null && _mountPointVar._var != null)
                return false; //接了 VarTransform，值要等 runtime 才知道
            mountPoint = MountPoint;
            return true;
        }

        /// <summary>
        /// 給 Editor 側驗證器讀 mount 的 target entity（被 attach 的對象）。
        /// 回 false 表示由 runtime 的 Var 決定，editor time 無法判斷、不該報錯。
        /// </summary>
        public bool TryGetEditorTargetEntity(out MonoEntity entity)
        {
            entity = null;
            if (_targetEntity != null && _targetEntity._var != null)
                return false; //接了 VarEntity，值要等 runtime 才知道
            //_parentEntity 是 [AutoParent]，editor time 不一定 cache 過，這裡自己抓
            entity = _targetEntity?.Value ?? GetComponentInParent<MonoEntity>();
            return true;
        }
#endif

        protected override void OnActionExecuteImplement()
        {
            var source = _sourceEntity.Value;
            var target = _targetEntity.Value ?? _parentEntity;

            if (source == null)
            {
                Debug.LogWarning("[MountViewRoot] Source entity is null", this);
                return;
            }

            if (target == null)
            {
                Debug.LogError("[MountViewRoot] Target entity is null", this);
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

            // 物理/collider 副作用集中在 ViewRoot.MountTo，連線同步由 NetworkedViewRoot 讀取結果
            source.ViewRoot.MountTo(target.ViewRoot, mountPoint.position, mountPoint.rotation,
                mountPoint, _handlePhysics, _disableColliders);

            Debug.Log(
                $"[MountViewRoot] Mounted {source.name} to {target.name} at {mountPoint.position} " +
                $"tick:{MonoFSM.Core.Simulate.WorldUpdateSimulator.CurrentTick}",
                this);
        }
    }
}
