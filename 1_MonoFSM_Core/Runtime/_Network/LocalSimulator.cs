using System;
using UnityEngine;

namespace MonoFSM_Core.Network
{
    [RequireComponent(typeof(UpdateSimulator))]
    public class LocalSimulator : MonoBehaviour
    {
        [Auto] private UpdateSimulator _simulator;

        //FIXME: fixed update呢？
        private void Update()
        {
            _simulator.Simulate(Time.deltaTime);
        }
    }
}