using System.Collections.Generic;
using System.Linq;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM_Core.Simulate
{
    //fixme: 還是要中心化註冊？怎麼做比較好？ cal
    public interface ISimulateRunner
    {
    }

    public sealed class UpdateSimulatorRunner : MonoBehaviour
    {
        [Required] [CompRef] [Auto] private ISimulateRunner _simulateRunner;
        private void Awake()
        {
            _simulateRunner = GetComponent<ISimulateRunner>();
            // _simulators.AddRange(_localSimulators); //不需要了？
        }

        public void RegisterUpdate(IUpdateSimulate target)
        {
            _simulators.Add(target);
        }

        public void UnregisterUpdate(IUpdateSimulate target)
        {
            _simulators.Remove(target);
        }

        //FIXME: 可能會動態移除
        [PreviewInInspector] [AutoChildren] private IUpdateSimulate[] _localSimulators;

        private readonly HashSet<IUpdateSimulate> _simulators = new(); //HashSet?
#if UNITY_EDITOR
        [PreviewInInspector] private IUpdateSimulate[] PreviewSimulators => _simulators.ToArray();
#endif
        
        

        /// <summary>
        /// 需要依照環境決定怎麼simulate
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            if (_simulators == null || _simulators.Count == 0)
            {
                Debug.LogWarning("No simulators found to simulate.");
                return;
            }

            foreach (var simulator in _simulators)
                if (simulator is { isActiveAndEnabled: true })
                    simulator.Simulate(deltaTime);
        }

        public void AfterUpdate()
        {
            if (_simulators == null || _simulators.Count == 0)
            {
                Debug.LogWarning("No simulators found to simulate in LateUpdate.");
                return;
            }

            foreach (var simulator in _simulators)
                if (simulator is { isActiveAndEnabled: true })
                    simulator.AfterUpdate();
                else
                    Debug.LogWarning("A simulator is null and cannot be simulated in LateUpdate.");
        }
    }
}