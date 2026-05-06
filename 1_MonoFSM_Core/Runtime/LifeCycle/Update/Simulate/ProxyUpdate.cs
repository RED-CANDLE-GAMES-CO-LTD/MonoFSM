using System;
using MonoFSM.Core.Simulate;
using UnityEngine;

namespace MonoFSM.Core.Update
{
    // UI, local端的
    /// <summary>
    /// 什麼時候會用到？
    /// </summary>
    [Obsolete]
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
