using System;
using UnityEngine;

namespace MonoFSM.Core.Simulate
{
    [DefaultExecutionOrder(10000)] //確保在所有Update之後執行
    [RequireComponent(typeof(WorldUpdateSimulator))]
    public class LocalSimulatorRunner : MonoBehaviour, ISimulateRunner
    {
        [Auto] private WorldUpdateSimulator _world;

        private void Start() //timing hmm
        {
            //FIXME: 還是要player生出來才呼叫？
            _world.WorldInit();
        }
        private void FixedUpdate()
        {
            _world.Simulate(Time.fixedDeltaTime);
        }
        //會需要Update嗎？

        private void LateUpdate()
        {
            _world.AfterUpdate();
        }

    }
}