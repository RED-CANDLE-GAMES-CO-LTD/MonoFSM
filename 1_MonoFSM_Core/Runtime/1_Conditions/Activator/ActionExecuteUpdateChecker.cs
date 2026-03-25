using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Condition
{
    //FIXME: 改名？
    public class ActionExecuteUpdateChecker : AbstractConditionUpdateChecker, IActionParent
    {
        //FIXME: 跑 renderAction?
        //Required?
        [CompRef] [AutoChildren] AbstractStateAction[] _actions;
        protected override void ActivateCheckImplement(bool isValid)
        {
            if (isValid)
            {
                foreach (var action in _actions)
                {
                    if (action != null && action.IsValid)
                        action.EventReceived();
                }
            }
        }
    }
}
