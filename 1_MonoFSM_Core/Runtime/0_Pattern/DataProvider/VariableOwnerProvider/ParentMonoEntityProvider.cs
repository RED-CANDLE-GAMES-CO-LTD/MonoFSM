using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    //Self?
    //改名，這個記不住，OwnerEntity? ParentEntity? 
    public class ParentMonoEntityProvider : MonoBehaviour, IMonoEntityProvider
    {
        //為什麼不會自己撈？
        [Required] [AutoParent] private MonoEntity _monoBlackboard;

        [PreviewInInspector] public MonoEntity monoEntity => _monoBlackboard;
        public MonoEntityTag entityTag => _monoBlackboard?.Tag;

        public T GetComponentOfOwner<T>()
        {
            return _monoBlackboard.GetComponent<T>();
        }
    }
}