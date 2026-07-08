namespace MonoFSM.Core
{
    /// <summary>
    /// 只觸發子層 IRenderBehaiour 的 OnEnterRender，由 State.OnExitStateRender 呼叫。
    /// 用於把離開時的 Render 效果（exit FX）獨立分群。
    /// </summary>
    public class OnStateExitRenderHandler : AbstractEventHandler, IRenderInvoker
    {
        //由 State.OnExitStateRender 在各端 Render 觸發，proxy 也會跑，不需要 render sync
        public override bool IsSimulateEventHandler => false;
    }
}
