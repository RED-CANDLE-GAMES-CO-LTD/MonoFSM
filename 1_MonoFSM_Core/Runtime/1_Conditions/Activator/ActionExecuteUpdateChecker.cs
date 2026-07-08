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
            //FIXME: 這個很危險耶，各種地方都要確保這件事？還是這個應該要refactor成eventHandler？
            if (!_parentObj.ShouldSimulte)
                return;
            if (isValid)
            {
                foreach (var action in _actions)
                {
                    //FIXME: 感覺 re-sim還是狂發耶，為什麼之前不會？
                    if (action != null && action.IsValid)
                        action.EventReceived();
                }
            }
        }
    }
}
