using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Runtime.Variable;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //這個也是 IBlackboardProvider的一種，會打架？
    //這個不該繼承VariableProviderRef？應該自己獨立？

    public class VarBlackboardRef : VariableProviderRef<VarEntity, MonoBlackboard>, IBlackboardProvider
    {
        public MonoBlackboard Blackboard => Value;

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