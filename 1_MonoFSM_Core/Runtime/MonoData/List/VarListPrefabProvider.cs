using MonoFSM.Core.DataProvider;
using MonoFSMCore.Runtime.LifeCycle;

namespace MonoFSM.Core.Variable.Providers
{
    //每一種list都得宣告一種provider, so stupid!
    public class VarListPrefabProvider : VariableProviderRef<VarListPrefab, MonoPoolObj>
    {
    }
}