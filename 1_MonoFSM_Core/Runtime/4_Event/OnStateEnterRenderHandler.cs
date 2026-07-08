namespace MonoFSM.Core
{
    /// <summary>
    /// 只觸發子層 IRenderBehaiour 的 OnEnterRender，由 State.OnEnterStateRender 呼叫。
    /// 用於把進入時的 Render 效果獨立分群，不影響 Simulate 端的 EventReceiver。
    /// </summary>
    public class OnStateEnterRenderHandler : AbstractEventHandler, IRenderInvoker
    {
        //由 State.OnEnterStateRender 在各端 Render 觸發，proxy 也會跑，不需要 render sync
        public override bool IsSimulateEventHandler => false;
    }
}
