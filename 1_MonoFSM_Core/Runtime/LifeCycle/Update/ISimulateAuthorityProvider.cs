namespace MonoFSMCore.Runtime.LifeCycle
{
    /// <summary>
    /// 讓 MonoObj 能在「不 reference 網路框架」的前提下，回頭查詢自己是否擁有模擬權限。
    /// Core 端只認識這個介面；由網路層（例如 Photon Fusion）掛一顆實作它的 component，
    /// MonoObj 透過 [Auto] 取得參考。單機/純 local 物件不會有實作者，MonoObj 會 fallback 到 _shouldSimulateFlag。
    /// </summary>
    public interface ISimulateAuthorityProvider
    {
        bool HasInputAuthority { get; }
        bool HasStateAuthority { get; }
    }
}
