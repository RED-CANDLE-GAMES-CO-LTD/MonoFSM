using RCGMaker.Core.DataProvider;
using MonoFSM.Variable;

namespace RCGMakerFSM.VarRef
{
    /// <summary>
    /// 可以拿到一個VarGameData的MonoBehaviour
    /// </summary>
    public class VarGameDataRef : VariableProviderRef<VarScriptableData, DescriptableData>, IGameDataProvider
    {
        public DescriptableData GameData => Value;
    }
}