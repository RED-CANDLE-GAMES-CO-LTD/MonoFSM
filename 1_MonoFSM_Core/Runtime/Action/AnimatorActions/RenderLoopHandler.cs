using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    public class RenderLoopHandler : AbstractEventHandler, IRenderSimulate
    {
        [AutoChildren] AbstractStateAction[] _stateActions;

        public void Render(float runnerLocalRenderTime)
        {
            foreach (var action in _stateActions)
            {
                action.OnActionRender();
            }
        }
    }
}
