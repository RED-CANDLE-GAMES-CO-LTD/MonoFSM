using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Action.AnimatorActions
{
    /// <summary>
    /// 每個 render frame 呼叫底下 [Render] 節點的 OnRender()。不吃 ShouldSimulte，各端（含 proxy）都會跑；
    /// 沒有 state 範圍、自己沒有 condition，要把關就掛在各 [Render] 節點的 _conditionGroup 上。
    /// 不繼承 AbstractEventHandler：那邊的 condition / authority gate / render sync 都是 event 觸發路徑用的，這裡用不到。
    /// </summary>
    public class RenderLoopHandler : AbstractDescriptionBehaviour, IRenderInvoker, IRenderUpdate
    {
        protected override string DescriptionTag => "Event";
        public override string Description => "RenderLoop";

        [CompRef] [ShowInInspector] [AutoChildren(DepthOneOnly = true)]
        private IRenderBehaiour[] _renderActions;

        [PreviewInDebugMode] private float _lastRenderEventTime = -1f;

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
