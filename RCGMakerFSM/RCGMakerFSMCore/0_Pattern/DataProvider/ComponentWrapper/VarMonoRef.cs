using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;

namespace RCGMakerFSM.RCGMakerFSMCore._0_Pattern.DataProvider.ComponentWrapper
{
    public class VarMonoRef : VariableProviderRef<VarMono, MonoDescriptable>, IVarMonoProvider
    {
        public DescriptableData SampleData => Variable.SampleData;
    }
}