using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;

namespace MonoFSM.VarRefOld
{
    /// <summary>
    /// 可以拿到一個VarGameData的MonoBehaviour
    /// </summary>
    public class VarDescriptableDataRef : VariableProviderRef<VarDescriptableData, DescriptableData>, IGameDataProvider
    {
        public DescriptableData GameData => Value;
    }
}