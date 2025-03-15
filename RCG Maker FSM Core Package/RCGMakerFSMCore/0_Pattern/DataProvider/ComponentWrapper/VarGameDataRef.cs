using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;

namespace RCG_Maker_FSM_Core_Package.RCGMakerFSMCore._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 可以拿到一個SODataVariable的MonoBehaviour
    /// </summary>
    public class VarGameDataRef : VariableProviderRef<VarGameData, DescriptableData>, IGameDataProvider
    {
        public DescriptableData GameData => Value;
    }
}