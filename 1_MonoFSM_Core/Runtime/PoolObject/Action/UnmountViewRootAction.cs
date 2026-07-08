using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 解除 source Entity 的 ViewRoot mount（停止跟隨目標），
    /// 可選擇同時還原物理狀態。
    /// </summary>
    public class UnmountViewRootAction : AbstractStateAction
    {
        [SerializeField] private VarEntityWrapper _sourceEntity;
        [SerializeField] private bool _handlePhysics = true;

        protected override void OnActionExecuteImplement()
        {
            var source = _sourceEntity.Value;

            if (source == null)
            {
                Debug.LogWarning("[UnmountViewRoot] Source entity is null", this);
                return;
            }

            if (source.ViewRoot == null)
            {
                Debug.LogWarning($"[UnmountViewRoot] Source {source.name} has no ViewRoot", this);
                return;
            }

            // 物理/collider 還原集中在 ViewRoot.Unmount，連線同步由 NetworkedViewRoot 讀取結果
            source.ViewRoot.Unmount(_handlePhysics);

            // Debug.Log($"[UnmountViewRoot] Unmounted {source.name}", this);
        }
    }
}
