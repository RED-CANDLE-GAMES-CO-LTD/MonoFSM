namespace MonoFSM.Core
{
    public interface IRenderSyncProvider : RenderInvoker
    {
        public void RequestRenderSync();
        public void RequestRenderSync<T>(T arg);
    }

    public interface RenderInvoker
    {
    }
}
