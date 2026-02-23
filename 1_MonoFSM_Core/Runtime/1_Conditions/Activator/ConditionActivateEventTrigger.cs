using MonoFSM.Core.Attributes;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Condition
{
    public class ConditionActivateEventTrigger : AbstractConditionActivateTarget
    {
        [CompRef] [Auto] private OnStateUpdateHandler _onStateUpdateHandler;

        protected override void ActivateCheckImplement(bool isValid)
        {
            if (isValid)
                _onStateUpdateHandler.EventHandle();
        }
    }
}
