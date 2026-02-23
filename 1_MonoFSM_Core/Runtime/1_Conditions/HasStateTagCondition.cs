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
        private StateMachineLogic _fsmLogic;

        [Required]
        [SerializeField]
        private StateTag _tag;

        protected override bool IsValid
        {
            get
            {
                if (_fsmLogic == null) return false;
                var current = _fsmLogic.CurrentState as GeneralState;
                return current != null && current.HasTag(_tag);
            }
        }

        public override string Description => $"Has Tag [{(_tag != null ? _tag.name : "?")}]";
    }
}
