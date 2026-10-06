using MonoDebugSetting;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Attributes;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.PhysicsAction
{
    /// <summary>
    /// 把指定 Rigidbody 的 isKinematic 設成 _isKinematic（抓起來解除 kinematic、被吃掉 / 固定時凍住物理）。
    /// 只改本機的 Rigidbody，不做網路同步；網路物件掛在只跑 SA 的 event 底下，位置交給 NetworkRigidbody 同步。
    /// </summary>
    public class SetRigidbodyKinematicAction : AbstractStateAction
    {
        [SerializeField] private Rigidbody _rigidbody;
        public bool _isKinematic = true;

        protected override void OnActionExecuteImplement()
        {
            // var rb = _rigidbodyProvider.Get();
            var rb = _rigidbody;
            if (rb != null)
            {
                rb.isKinematic = _isKinematic;
                if (RuntimeDebugSetting.IsDebugMode)
                    Debug.Log($"Set Rigidbody Kinematic: {rb.name} to {_isKinematic}",
                        rb.gameObject);
            }
            else
            {
                Debug.LogError("Rigidbody not found in SetRigidbodyKinematicAction", this);
            }
        }
    }
}
