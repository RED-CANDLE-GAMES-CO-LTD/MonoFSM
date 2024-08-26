using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RCGMaker.Core
{
    public interface IProxyUpdate
    {
        GameObject gameObject { get; }
        void UpdateProxy();
    }

    public interface IProxyLateUpdate
    {
        GameObject gameObject { get; }
        void LateUpdateProxy();
    }

    public class UpdateLoopManager : SingletonBehaviour<UpdateLoopManager>, IClearReference
    {
        private List<IProxyUpdate> updateList = new();
        private List<IProxyLateUpdate> lateUpdateList = new();

        public void RegisterUpdate(IProxyUpdate updateTarget)
        {
            updateList.Add(updateTarget);
        }

        public void RegisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            lateUpdateList.Add(updateTarget);
        }

        public void UnregisterUpdate(IProxyUpdate updateTarget)
        {
            updateList.Remove(updateTarget);
        }

        public void UnregisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            lateUpdateList.Remove(updateTarget);
        }

        private void Update()
        {
            foreach (var updateTarget in updateList)
            {
                Profiler.BeginSample("updateTarget", updateTarget.gameObject);
                updateTarget.UpdateProxy();
                Profiler.EndSample();
            }
        }

        private void LateUpdate()
        {
            foreach (var lateUpdateTarget in lateUpdateList)
            {
                Profiler.BeginSample("lateUpdateTarget", lateUpdateTarget.gameObject);
                lateUpdateTarget.LateUpdateProxy();
                Profiler.EndSample();
            }
        }

        public void ClearReference()
        {
        }
    }
}