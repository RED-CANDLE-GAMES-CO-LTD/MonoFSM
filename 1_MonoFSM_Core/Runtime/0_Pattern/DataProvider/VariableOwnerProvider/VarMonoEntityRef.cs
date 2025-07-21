using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Runtime.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //這個也是 IBlackboardProvider的一種，會打架？
    //這個不該繼承VariableProviderRef？應該自己獨立？

    [Obsolete]
    public class VarMonoEntityRef : VariableProviderRef<VarEntity, MonoEntity>, IMonoEntityProvider
    {
        public MonoEntity monoEntity => Value;
        public MonoEntityTag entityTag => Value?.Tag;

        public T GetComponentOfOwner<T>()
        {
            if (Value == null)
            {
                Debug.LogError("VariableOwner is null, cannot get component.");
                return default;
            }

            // 這裡的Value是MonoBlackboard，應該可以直接調用GetComponent
            return Value.GetComponent<T>();
        }
    }
}