using MonoFSM.Core.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core
{
    public class IsStateCondition: AbstractConditionBehaviour
    {
        [PreviewInInspector]
        [AutoParent] StateMachineOwner _owner;
        [DropDownRef]
        [SerializeField]GeneralState _targetState;

        protected override bool IsValid => _owner.fsmLogic.IsCurrentState(_targetState);

        //_owner.FsmContext.currentStateType == _targetState;
        public override string Description => $"{GetType().Name}({_targetState.name})";
    }
}