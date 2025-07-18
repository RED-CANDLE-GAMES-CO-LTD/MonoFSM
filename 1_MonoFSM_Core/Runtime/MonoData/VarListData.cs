using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Variable;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    public class VarListData : VarList<DescriptableData>, IGameDataProvider
    {
        public DescriptableData GameData => CurrentObj;
    }
}