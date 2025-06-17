using System;
using System.Collections.Generic;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Deprecated
{
    /// <summary>
    /// FIXME: 要做成singleton嗎？
    /// </summary>
    [Obsolete]
    public class StateMachineManager : MonoBehaviour, IUpdateSimulate //, IBackToMenuDestroy
    {
        public static StateMachineManager Instance => _instance;
        private static StateMachineManager _instance;

        public static bool IsAvailable()
        {
            return _instance != null && _instance.gameObject != null && _instance.gameObject.activeInHierarchy;
        }

        private void Awake()
        {
            _instance = this;
            // allRunners = new List<StateMachineRunner>();
        }

        [ShowInInspector] private readonly List<StateMachineRunner> _allRunners = new();
        public bool EnableUpdate = true;
        public bool EnableFixedUpdate = true;
        public bool EnableLateUpdate = true;

        public StateMachineManager Register(StateMachineRunner runner)
        {
            _allRunners.Add(runner);
            return this;
        }

        public StateMachineManager Unregister(StateMachineRunner runner)
        {
            _allRunners.Remove(runner);
            return this;
        }

        
        private void Update()
        {
            if (!EnableUpdate) return;
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

        private void FixedUpdate()
        {
            if (!EnableFixedUpdate) return;
            for (var index = _allRunners.Count - 1; index >= 0; index--)
            {
                var runner = _allRunners[index];
                // if (runner.isActiveAndEnabled)
                runner.FixedUpdateFromManager();
            }
        }

        private void LateUpdate()
        {
            if (!EnableLateUpdate) return;
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

        public void Simulate(float deltaTime)
        {
            if (!IsAvailable()) return;
            for (var index = _allRunners.Count - 1; index >= 0; index--)
            {
                var runner = _allRunners[index];
                // if (runner.isActiveAndEnabled)
                runner.Simulate(deltaTime);
            }
        }

        public void AfterUpdate()
        {
            if (!IsAvailable()) return;
            for (var index = _allRunners.Count - 1; index >= 0; index--)
            {
                var runner = _allRunners[index];
                // if (runner.isActiveAndEnabled)
                runner.LateUpdateFromManager();
            }
        }
    }
}

