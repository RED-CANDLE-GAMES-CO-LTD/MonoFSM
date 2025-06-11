using System;
using UnityEngine;

namespace MonoFSM.Core.Simulate
{
    //FIXME: 這個要放在哪？
    [DefaultExecutionOrder(10000)] //確保在所有Update之後執行
    [RequireComponent(typeof(WorldUpdateSimulator))]
    public class LocalSimulator : MonoBehaviour, ISimulateRunner
    {
        [Auto] private WorldUpdateSimulator _world;

        //FIXME: fixed update呢？
        private void Update()
        {
            _world.Simulate(Time.deltaTime);
        }

        private void LateUpdate()
        {
            _world.AfterUpdate();
        }

        //要比spawn還晚？還是?
        private void Start() //timing hmm
        {
            //FIXME: 還是要player生出來才呼叫？
            _world.WorldInit();
        }
    }
}