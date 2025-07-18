using MonoFSM.Runtime;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.Variable
{
    //還是要吃scriptableObject, 如果不會動的話，collection
    public class VarListPrefab : VarList<MonoPoolObj>, ICompProvider<MonoPoolObj> //, IPrefabSerializeCacheOwner
    {
        public MonoPoolObj Get()
        {
            return CurrentObj;
        }
    }
}