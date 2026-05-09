namespace MonoFSM.Core
{
    /// <summary>
    /// 給連線用的 render 非同步呼叫，暫存資料等 Render Loop 來顯示特效用 (VFX/SFX)，避免 re-sim重複觸發, proxy也要可以拿到
    /// </summary>
    public interface IRenderSyncProvider : IRenderInvoker
    {
        public void RequestRenderSync();
        public void RequestRenderSync<T>(T arg);
    }

    public interface IRenderInvoker
    {
    }
}
