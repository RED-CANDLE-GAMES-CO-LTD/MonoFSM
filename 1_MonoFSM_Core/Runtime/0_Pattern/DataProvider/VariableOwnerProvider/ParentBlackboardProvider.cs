using MonoFSM.Core.Attributes;
using MonoFSM.Runtime.FSM.RCGStateMachine;
using UnityEngine;

namespace MonoFSM.Core.Runtime
{
    public class ParentBlackboardProvider : MonoBehaviour, IBlackboardProvider
    {
        [AutoParent] private MonoBlackboard _monoBlackboard;

        [PreviewInInspector] public MonoBlackboard Blackboard => _monoBlackboard;

        public T GetComponentOfOwner<T>()
        {
            return _monoBlackboard.GetComponent<T>();
        }
    }
}