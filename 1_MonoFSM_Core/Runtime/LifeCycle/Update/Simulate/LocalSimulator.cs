using System;
using UnityEngine;

namespace MonoFSM_Core.Simulate
{
    //FIXME: 這個要放在哪？
    [RequireComponent(typeof(UpdateSimulatorRunner))]
    public class LocalSimulator : MonoBehaviour, ISimulateRunner
    {
        [Auto] private UpdateSimulatorRunner _simulatorRunner;

        //FIXME: fixed update呢？
        private void Update()
        {
            _simulatorRunner.Simulate(Time.deltaTime);
        }

        private void LateUpdate()
        {
            _simulatorRunner.AfterUpdate();
        }
    }
}