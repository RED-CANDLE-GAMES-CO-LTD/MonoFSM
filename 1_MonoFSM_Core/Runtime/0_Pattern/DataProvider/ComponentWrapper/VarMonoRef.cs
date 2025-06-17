using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;

namespace MonoFSM.VarRef
{
    public class VarMonoRef : VariableProviderRef<VarMono, MonoDescriptable>, IVarMonoProvider
    {
        public DescriptableData SampleData => Variable.SampleData;
        
    }
}