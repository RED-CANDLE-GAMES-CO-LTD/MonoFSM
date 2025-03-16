using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;

namespace RCGMakerFSM.VarRef
{
    /// <summary>
    /// 可以拿到一個VarGameData的MonoBehaviour
    /// </summary>
    public class VarGameDataRef : VariableProviderRef<VarGameData, DescriptableData>, IGameDataProvider
    {
        public DescriptableData GameData => Value;
    }
}