using Fusion.Addons.FSM;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    public class IsStateCondition : AbstractConditionBehaviour
    {
        [Required]
        [DropDownRef]
        [SerializeField]
        GeneralState _targetState;

        protected override bool IsValid =>
            _targetState != null && _targetState.Owner != null &&
            _targetState.Owner.IsCurrentState(_targetState);

        //_owner.FsmContext.currentStateType == _targetState;
        public override string Description => $"Is {_targetState?.name}";

        protected override bool HasError()
        {
            return base.HasError() && _targetState != null && _targetState.isActiveAndEnabled;
        }
    }
}
