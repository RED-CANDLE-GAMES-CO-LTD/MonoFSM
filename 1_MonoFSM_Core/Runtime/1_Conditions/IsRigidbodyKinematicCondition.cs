using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Condition
{
    /// <summary>
    /// 檢查指定 Rigidbody 是否為 isKinematic。Rigidbody 沒指到時視為 false。
    /// </summary>
    public class IsRigidbodyKinematicCondition : AbstractConditionBehaviour
    {
        [Required] [SerializeField] private Rigidbody _rigidbody;

        [ShowInInspector] [ReadOnly] private string _failReason;

        protected override bool IsValid
        {
            get
            {
                if (_rigidbody == null)
                {
                    _failReason = "_rigidbody is null";
                    return false;
                }

                _failReason = null;
                return _rigidbody.isKinematic;
            }
        }
    }
}
