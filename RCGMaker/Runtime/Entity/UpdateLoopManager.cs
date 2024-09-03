using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RCGMaker.Core
{
    public class UpdateLoopManager : SingletonBehaviour<UpdateLoopManager>, ILevelAwake, IGameDestroy
    {
        public readonly UpdateList<IProxyUpdate> UpdateList = new((t) => t.UpdateProxy());
        public readonly UpdateList<IProxyLateUpdate> LateUpdateList = new((t) => t.LateUpdateProxy());

        private void Update()
        {
            UpdateList.UpdateManual();
        }

        private void LateUpdate()
        {
            LateUpdateList.UpdateManual();
        }

        public void EnterLevelAwake()
        {
            // ClearReference();
            UpdateList.ClearNull();
            LateUpdateList.ClearNull();
        }

        public void OnGameDestroy()
        {
            UpdateList.ClearRef();
            LateUpdateList.ClearRef();
        }
    }
}