using System;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM_Core.Network
{
    //fixme: 還是要中心化註冊？怎麼做比較好？ cal
    public interface ISimulateRunner
    {
    }
    public class UpdateSimulator : MonoBehaviour //fixme: 不能用繼承的...
    {
        [PreviewInInspector] [AutoChildren] private IUpdateSimulate[] _simulators;

        [Required] [CompRef] [Auto] private ISimulateRunner _simulateRunner;

        /// <summary>
        /// 需要依照環境決定怎麼simulate
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            if (_simulators == null || _simulators.Length == 0)
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
            if (_simulators == null || _simulators.Length == 0)
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