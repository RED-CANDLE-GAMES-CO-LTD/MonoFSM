using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    //強制切到指定State，無視Transition/Condition，走RestoreState機制避免被網路同步覆蓋
    public class ForceChangeToStateAction : AbstractStateAction
    {
        [Required] [DropDownRef] [SerializeField] private GeneralState _targetState;

        public override string Description => $"Force => {(_targetState != null ? _targetState.name : "?")}";

        protected override void OnActionExecuteImplement()
        {
            if (_targetState == null || _targetState.Owner == null)
            {
                Debug.LogError("ForceChangeToStateAction: _targetState or Owner is null", this);
                return;
            }

            Debug.Log($"ForceChangeToState to {_targetState.Name}", this);
            _targetState.Owner.RestoreState(_targetState.StateId);
        }
    }
}
