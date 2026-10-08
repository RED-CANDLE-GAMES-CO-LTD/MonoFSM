using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace MonoFSM.Core.Condition
{
    //FIXME: 改名？
    /// <summary>
    ///  每個 Simulate tick 判斷子節點的 [If] 條件，成立就執行子節點的 Action（例：結算畫面「host 按 Enter → 回大廳」）。
    ///  跑在 Simulate，只在 ShouldSimulte 的端（通常是 host / state authority）會執行；條件連續成立會每個 tick 都觸發，
    ///  一次性的觸發要靠條件本身（例如「這個 tick 剛按下」）。
    /// </summary>
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
