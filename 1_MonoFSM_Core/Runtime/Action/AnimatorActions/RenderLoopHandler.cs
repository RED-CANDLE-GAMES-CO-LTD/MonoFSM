using MonoFSM.Core;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    /// <summary>
    /// 每個 render frame 呼叫底下 [Render] 節點的 OnRender()。不吃 ShouldSimulte，各端（含 proxy）都會跑；
    /// 沒有 state 範圍，要自己用 _conditionFolder 把關。
    /// </summary>
    //FIXME: 乾淨的AbstractDescriptor就好？
    public class RenderLoopHandler : AbstractEventHandler, IRenderUpdate
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
