using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;

namespace RCGMakerFSMCore.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class VarBoolProviderRef : VariableProviderRef<VarBool, bool>, IBoolProvider
    {
        public bool IsTrue => Value;
    }
}