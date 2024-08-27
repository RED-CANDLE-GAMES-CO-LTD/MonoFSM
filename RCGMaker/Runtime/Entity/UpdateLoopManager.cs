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

    public class UpdateList<T> where T : ICustomUpdate
    {
        private Action<T> _updateAction;

        public UpdateList(Action<T> updateAction)
        {
            _updateAction = updateAction;
        }

        private HashSet<T> _updateSet = new();
        private List<T> updateList = new();
        private List<T> toUnregisterUpdateList = new();

        public void Register(T updateTarget)
        {
            updateList.Add(updateTarget);
        }

        public void Unregister(T updateTarget)
        {
            toUnregisterUpdateList.Add(updateTarget);
        }

        public void UpdateManual()
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
                _updateAction(updateTarget);
                Profiler.EndSample();
            }
        }
    }

    public class UpdateLoopManager : SingletonBehaviour<UpdateLoopManager>, IClearReference
    {
        public readonly UpdateList<IProxyUpdate> UpdateList = new((t) => t.UpdateProxy());
        public readonly UpdateList<IProxyLateUpdate> LateUpdateList = new((t) => t.LateUpdateProxy());

        // private HashSet<IProxyUpdate> _updateSet = new();
        // private List<IProxyUpdate> updateList = new();
        // private List<IProxyUpdate> toUnregisterUpdateList = new();
        //
        // private HashSet<IProxyLateUpdate> _lateUpdateSet = new();
        // private List<IProxyLateUpdate> lateUpdateList = new();
        // private List<IProxyLateUpdate> toUnregisterLateUpdateList = new();

        [Obsolete]
        public void RegisterUpdate(IProxyUpdate updateTarget)
        {
            // _updateList.Add(updateTarget);
            UpdateList.Register(updateTarget);
            // toUnregisterUpdateList.Add(updateTarget);
            // updateList.Add(updateTarget);
        }

        [Obsolete]
        public void UnregisterUpdate(IProxyUpdate updateTarget)
        {
            
            // updateList.Remove(updateTarget);
            // toUnregisterUpdateList.Add(updateTarget);
            UpdateList.Unregister(updateTarget);
        }

        [Obsolete]
        public void RegisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            // toUnregisterLateUpdateList.Add(updateTarget);
            // _lateUpdateList.Add(updateTarget);
            LateUpdateList.Register(updateTarget);
        }

        [Obsolete]
        public void UnregisterLateUpdate(IProxyLateUpdate updateTarget)
        {
            // lateUpdateList.Remove(updateTarget);
            // toUnregisterLateUpdateList.Add(updateTarget);
            LateUpdateList.Unregister(updateTarget);
        }

        private void Update()
        {
            UpdateList.UpdateManual();
            // foreach (var updateTarget in _updateList)
            // {
            //     _updateSet.Add(updateTarget);
            // }
            //
            // foreach (var updateTarget in toUnregisterUpdateList)
            // {
            //     _updateSet.Remove(updateTarget);
            // }
            //
            // toUnregisterUpdateList.Clear();
            // _updateList.Clear();
            //
            // foreach (var updateTarget in _updateSet)
            // {
            //     Profiler.BeginSample("updateTarget", updateTarget.gameObject);
            //     updateTarget.UpdateProxy();
            //     Profiler.EndSample();
            // }
        }

        private void LateUpdate()
        {
            LateUpdateList.UpdateManual();
            // foreach (var lateUpdateTarget in _lateUpdateList)
            // {
            //     _lateUpdateSet.Add(lateUpdateTarget);
            // }
            //
            // foreach (var lateUpdateTarget in toUnregisterLateUpdateList)
            // {
            //     _lateUpdateSet.Remove(lateUpdateTarget);
            // }
            //
            // _lateUpdateList.Clear();
            // toUnregisterLateUpdateList.Clear();
            //
            // foreach (var lateUpdateTarget in _lateUpdateSet)
            // {
            //     Profiler.BeginSample("lateUpdateTarget", lateUpdateTarget.gameObject);
            //     lateUpdateTarget.LateUpdateProxy();
            //     Profiler.EndSample();
            // }
        }

        public void ClearReference()
        {
        }
    }
}