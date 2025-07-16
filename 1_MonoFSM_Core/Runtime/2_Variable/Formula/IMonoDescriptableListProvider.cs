using System.Collections.Generic;
using MonoFSM.Runtime;

namespace MonoFSM.Core.Formula
{
    public interface IMonoDescriptableListProvider
    {
        IEnumerable<MonoEntity> GetDescriptables();
    }
}
