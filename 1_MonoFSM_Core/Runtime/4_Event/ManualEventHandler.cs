namespace MonoFSM.Core
{
    /// <summary>
    /// 不綁定任何觸發來源，純粹透過外部引用呼叫 EventHandle() 的 EventHandler。
    /// </summary>
    public class ManualEventHandler : AbstractEventHandler
    {
    }
}
