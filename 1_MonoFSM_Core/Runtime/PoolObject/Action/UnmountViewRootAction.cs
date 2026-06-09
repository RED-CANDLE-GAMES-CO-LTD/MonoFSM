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

            source.UnmountViewRoot();

            // 還原物理狀態
            if (_handlePhysics)
            {
                var rb = source.GetCompCache<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }

                // 還原 Mount 時關掉的 colliders（Mount 沒關就是 no-op）
                source.ViewRoot?.RestoreCollidersAfterUnmount();
            }

            // Debug.Log($"[UnmountViewRoot] Unmounted {source.name}", this);
        }
    }
}
