using System;
using System.Collections.Generic;

namespace RCGMaker.Core
{
    public class StateMachineManager : SingletonBehaviour<StateMachineManager>
    {
        private void Awake()
        {
            InSceneAwake(this);
            // allRunners = new List<StateMachineRunner>();
        }

        private readonly List<StateMachineRunner> _allRunners = new();

        public void Register(StateMachineRunner runner)
        {
            _allRunners.Add(runner);
        }

        public void Unregister(StateMachineRunner runner)
        {
            _allRunners.Remove(runner);
        }

        private void Update()
        {
            for (var index = _allRunners.Count - 1; index >= 0; index--)
            {
                var runner = _allRunners[index];
                // if (runner.isActiveAndEnabled)
                runner.UpdateFromManager();
            }
        }

        private void LateUpdate()
        {
            for (var index = _allRunners.Count - 1; index >= 0; index--)
            {
                var runner = _allRunners[index];
                // if (runner.isActiveAndEnabled)
                runner.LateUpdateFromManager();
            }
        }
    }
}