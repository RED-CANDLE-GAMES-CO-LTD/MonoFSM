using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    //FIXME: 乾淨的AbstractDescriptor就好？
    public class RenderLoopHandler : AbstractEventHandler, IRenderSimulate
    {
        [ShowInInspector] [AutoChildren(DepthOneOnly = true)]
        AbstractStateAction[] _renderActions;

        public void Render(float runnerLocalRenderTime)
        {
            _lastEventHandledTime = Time.time;
            foreach (var action in _renderActions)
            {
                action.OnActionRender();
            }
        }
    }
}
