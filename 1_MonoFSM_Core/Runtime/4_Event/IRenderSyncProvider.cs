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

    /// <summary>
    /// 陣列版 render sync：一顆掛在 NetworkObject root，多個 EventHandler 共用。
    /// 和 IRenderSyncProvider（1:1 同物件版）的差別是呼叫端要帶自己的身分。
    /// 注意：刻意不繼承 IRenderInvoker，避免被 AbstractRenderBehaviour 的 [AutoParent] 誤抓。
    /// </summary>
    public interface IRenderSyncHub
    {
        public void RequestRenderSync(AbstractEventHandler handler);
        public void RequestRenderSync<T>(AbstractEventHandler handler, T arg);
    }

    public interface IRenderInvoker
    {
    }
}
