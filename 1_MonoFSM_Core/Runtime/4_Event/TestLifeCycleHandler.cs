using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// Test implementation of AbstractStateLifeCycleHandler to verify the new architecture works correctly.
    /// This can be used as a reference for creating custom lifecycle handlers.
    /// </summary>
    public class TestLifeCycleHandler : AbstractStateLifeCycleHandler
    {
        [Header("Test Settings")]
        [SerializeField] private bool _logStateChanges = true;

        protected override void OnStateEnter()
        {
            if (_logStateChanges)
                Debug.Log($"[{name}] State Enter", this);
            
            // Call base to execute any child event receivers
            base.OnStateEnter();
        }

        protected override void OnStateUpdate()
        {
            if (_logStateChanges)
                Debug.Log($"[{name}] State Update", this);
            
            // Call base to execute any child event receivers
            base.OnStateUpdate();
        }

        protected override void OnStateExit()
        {
            if (_logStateChanges)
                Debug.Log($"[{name}] State Exit", this);
            
            // Call base to execute any child event receivers
            base.OnStateExit();
        }
    }
}