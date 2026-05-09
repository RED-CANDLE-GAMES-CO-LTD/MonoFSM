namespace MonoFSM.Core
{
    /// <summary>
    /// 只觸發子層 IRenderBehaiour 的 OnEnterRender，由 State.OnExitStateRender 呼叫。
    /// 用於把離開時的 Render 效果（exit FX）獨立分群。
    /// </summary>
    public class OnStateExitRenderHandler : AbstractEventHandler, IRenderInvoker
    {
    }
}
