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
        public void Render(float runnerLocalRenderTime)
        {
            _lastRenderEventTime = Time.time;
            foreach (var action in _renderActions)
            {
                action.OnRender();
            }
        }
    }
}
