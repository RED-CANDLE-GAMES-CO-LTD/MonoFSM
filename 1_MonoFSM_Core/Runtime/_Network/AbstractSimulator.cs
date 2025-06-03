using RCGMaker.Core.Attributes;
using UnityEngine;

namespace MonoFSM_Core.Network
{
    //fixme: 還是要中心化註冊？怎麼做比較好？
    public class AbstractSimulator : MonoBehaviour //fixme: 不能用繼承的...
    {
        [PreviewInInspector] [AutoChildren] private IUpdateSimulate[] _simulators;

        public void Simulate(float deltaTime)
        {
            if (_simulators == null || _simulators.Length == 0)
            {
                Debug.LogWarning("No simulators found to simulate.");
                return;
            }

            foreach (var simulator in _simulators)
                if (simulator != null)
                    simulator.Simulate(deltaTime);
                else
                    Debug.LogWarning("A simulator is null and cannot be simulated.");
        }
    }
}