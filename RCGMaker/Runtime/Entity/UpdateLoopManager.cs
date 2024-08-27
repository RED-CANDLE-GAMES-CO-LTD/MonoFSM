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

        private List<IProxyUpdate> toUnregisterUpdateList = new();
        
        private List<IProxyLateUpdate> lateUpdateList = new();

        private HashSet<IProxyUpdate> _updateSet = new();
        private HashSet<IProxyLateUpdate> _lateUpdateSet = new();

        private List<IProxyLateUpdate> toUnregisterLateUpdateList = new();

        
        public void RegisterUpdate(IProxyUpdate updateTarget)
        {
            updateList.Add(updateTarget);
            // toUnregisterUpdateList.Add(updateTarget);
            // updateList.Add(updateTarget);
        }

        public void RegisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            // toUnregisterLateUpdateList.Add(updateTarget);
            lateUpdateList.Add(updateTarget);
        }

        public void UnregisterUpdate(IProxyUpdate updateTarget)
        {
            // updateList.Remove(updateTarget);
            toUnregisterUpdateList.Add(updateTarget);
        }

        public void UnregisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            // lateUpdateList.Remove(updateTarget);
            toUnregisterLateUpdateList.Add(updateTarget);
        }

        private void Update()
        {
            foreach (var updateTarget in updateList)
            {
                _updateSet.Add(updateTarget);
            }

            foreach (var updateTarget in toUnregisterUpdateList)
            {
                _updateSet.Remove(updateTarget);
            }

            toUnregisterUpdateList.Clear();
            updateList.Clear();

            foreach (var updateTarget in _updateSet)
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
                _lateUpdateSet.Add(lateUpdateTarget);
            }

            foreach (var lateUpdateTarget in toUnregisterLateUpdateList)
            {
                _lateUpdateSet.Remove(lateUpdateTarget);
            }

            lateUpdateList.Clear();
            toUnregisterLateUpdateList.Clear();

            foreach (var lateUpdateTarget in toUnregisterLateUpdateList)
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