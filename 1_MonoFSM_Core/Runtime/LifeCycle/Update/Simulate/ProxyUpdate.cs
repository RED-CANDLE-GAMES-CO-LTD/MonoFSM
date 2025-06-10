using System;
using MonoFSM_Core.Simulate;
using UnityEngine;

namespace MonoFSM_Core.Update
{
    // UI, local端的
    public class ProxyUpdate : MonoBehaviour //會和local simulator衝突嗎？
    {
        //FIXME: 如果parent有local simulator就把自己關掉？
        [Auto] private IUpdateSimulate _updateSimulate;

        private void Update()
        {
            _updateSimulate.Simulate(Time.deltaTime);
        }
    }
}