using System.Collections.Generic;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Formula;
using MonoFSM.Runtime;

namespace MonoFSM.Core.Variable.Providers
{
    //variable 對 value... list?
    public class VarListEntityProvider : VariableProviderRef<VarListEntity, List<MonoEntity>>,
        IMonoDescriptableListProvider
    {
        //FIXME: VarListEntity不一定用List?
        public IEnumerable<MonoEntity> GetDescriptables()
        {
            if (Variable == null) return new List<MonoEntity>();
            // VarListEntity stores items as MonoDescriptable.
            return Variable.GetItems();
        }
    }
}