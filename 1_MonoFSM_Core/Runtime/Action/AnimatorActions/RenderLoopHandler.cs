using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    //FIXME: 乾淨的AbstractDescriptor就好？
    public class RenderLoopHandler : AbstractEventHandler, IRenderUpdate, IRenderInvoker
    {
        //每幀 Render 驅動，各端自己跑，不需要 render sync
        public override bool IsSimulateEventHandler => false;

        public void Render(float runnerLocalRenderTime)
        {
            _lastRenderEventTime = Time.time;
            foreach (var action in _renderActions)
            {
                if (!action.isActiveAndEnabled) continue;
                action.OnRender();
            }
        }
    }
}
