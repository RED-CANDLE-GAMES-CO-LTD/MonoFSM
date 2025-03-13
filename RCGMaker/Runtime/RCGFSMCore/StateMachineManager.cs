using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace RCGMaker.Core
{
    public class StateMachineManager : SingletonBehaviour<StateMachineManager>, IBackToMenuDestroy
    {
        private void Awake()
        {
            InSceneAwake(this);
            // allRunners = new List<StateMachineRunner>();
        }

        [ShowInInspector]
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
            //好噁，回主選單要清乾淨？不想要update一直檢查
            if (!IsAvailable())
                _allRunners.RemoveAll(runner => runner == null);
            //scene loaded?
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

        public void BackToTitle()
        {
            _allRunners.Clear();
        }
    }
}