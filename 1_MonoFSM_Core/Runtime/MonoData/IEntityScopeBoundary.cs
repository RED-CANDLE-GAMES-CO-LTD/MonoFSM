namespace MonoFSM.Runtime
{
    /// <summary>
    /// 標記：entity 的 folder 掃描（<c>MonoBlackboard</c> 的 VariableFolder / StateFolder / EffectDetectable / SchemaFolder）
    /// 遇到掛著這個介面的節點就不往下鑽，也不收那顆節點本身。
    /// 實作者：<see cref="MonoEntity" />（nested entity 的 folder 歸它自己）、MonoModulePack（pack 的 folder 走
    /// BindModulePackFolders 併進來，不該被宿主直接搶成自己的）、ModulePackGeometry（pack 的幾何 runtime 會被搬進宿主 ViewRoot，
    /// 裡面的 detect target / 判定 collider 歸 pack）。
    /// 掃描的起點（宿主自己）永遠不算邊界。
    /// </summary>
    public interface IEntityScopeBoundary
    {
    }
}
