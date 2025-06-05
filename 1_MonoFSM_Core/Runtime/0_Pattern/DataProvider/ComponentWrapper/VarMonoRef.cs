using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;

namespace MonoFSM.VarRef
{
    public class VarMonoRef : VariableProviderRef<VarMono, MonoDescriptable>, IVarMonoProvider
    {
        public DescriptableData SampleData => Variable.SampleData;
        
    }
}