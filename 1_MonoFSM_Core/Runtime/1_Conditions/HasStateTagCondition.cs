using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using Fusion.Addons.FSM;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    public class HasStateTagCondition : AbstractConditionBehaviour
    {
        [Required]
        [DropDownRef]
        [SerializeField]
        private MonoFSMOwner _fsmOwner;

        [Required]
        [SerializeField]
        private StateTag _tag;

        protected override bool IsValid
        {
            get
            {
                if (_fsmOwner == null) return false;
                var current = _fsmOwner.CurrentState as GeneralState;
                return current != null && current.HasTag(_tag);
            }
        }

        public override string Description => $"Has Tag [{(_tag != null ? _tag.name : "?")}]";
    }
}
