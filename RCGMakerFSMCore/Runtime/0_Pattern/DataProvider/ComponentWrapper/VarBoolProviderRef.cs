using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime.FSM._2_Variable;

namespace RCGMakerFSMCore.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class VarBoolProviderRef: VariableProviderRef<VarBool, bool>,IBoolProvider
    {
        public bool IsTrue => Value;
    }
}