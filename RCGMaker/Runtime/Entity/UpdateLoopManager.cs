using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RCGMaker.Core
{
    public interface IProxyUpdate : ICustomUpdate
    {
        void UpdateProxy();
    }

    public interface IProxyLateUpdate : ICustomUpdate
    {
        void LateUpdateProxy();
    }

    public interface ICustomUpdate
    {
        GameObject gameObject { get; }
    }

    // public class UpdateList<T> where T : ICustomUpdate
    // {
    //     public UpdateList(delegate updateFunc)
    //     {
    //         
    //     }
    //     private HashSet<T> _updateSet = new();
    //     private List<T> updateList = new();
    //     private List<T> toUnregisterUpdateList = new();
    //
    //     public void RegisterUpdate(T updateTarget)
    //     {
    //         updateList.Add(updateTarget);
    //     }
    //
    //     public void UnregisterUpdate(T updateTarget)
    //     {
    //         toUnregisterUpdateList.Add(updateTarget);
    //     }
    //
    //     public void Update()
    //     {
    //         foreach (var updateTarget in updateList)
    //         {
    //             _updateSet.Add(updateTarget);
    //         }
    //
    //         foreach (var updateTarget in toUnregisterUpdateList)
    //         {
    //             _updateSet.Remove(updateTarget);
    //         }
    //
    //         toUnregisterUpdateList.Clear();
    //         updateList.Clear();
    //         foreach (var updateTarget in _updateSet)
    //         {
    //             Profiler.BeginSample("updateTarget", updateTarget.gameObject);
    //             updateTarget.UpdateProxy();
    //             Profiler.EndSample();
    //         }
    //     }
    // }

    public class UpdateLoopManager : SingletonBehaviour<UpdateLoopManager>, IClearReference
    {
        private HashSet<IProxyUpdate> _updateSet = new();
        private List<IProxyUpdate> updateList = new();
        private List<IProxyUpdate> toUnregisterUpdateList = new();
        
        private HashSet<IProxyLateUpdate> _lateUpdateSet = new();
        private List<IProxyLateUpdate> lateUpdateList = new();
        private List<IProxyLateUpdate> toUnregisterLateUpdateList = new();

        
        public void RegisterUpdate(IProxyUpdate updateTarget)
        {
            updateList.Add(updateTarget);
            // toUnregisterUpdateList.Add(updateTarget);
            // updateList.Add(updateTarget);
        }


        public void UnregisterUpdate(IProxyUpdate updateTarget)
        {
            
            // updateList.Remove(updateTarget);
            toUnregisterUpdateList.Add(updateTarget);
        }

        public void RegisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            // toUnregisterLateUpdateList.Add(updateTarget);
            lateUpdateList.Add(updateTarget);
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

            foreach (var lateUpdateTarget in _lateUpdateSet)
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