using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //Self?
    //改名，這個記不住，OwnerEntity? ParentEntity? 
    public class ParentBlackboardProvider : MonoBehaviour, IBlackboardProvider
    {
        //為什麼不會自己撈？
        [Required]
        [AutoParent] private MonoBlackboard _monoBlackboard;

        [PreviewInInspector] public MonoBlackboard Blackboard => _monoBlackboard;

        public T GetComponentOfOwner<T>()
        {
            return _monoBlackboard.GetComponent<T>();
        }
    }
}