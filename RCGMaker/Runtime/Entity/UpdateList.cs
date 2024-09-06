using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core.Attributes;
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
        private HashSet<T> updateList = new();
        private HashSet<T> toUnregisterUpdateList = new();
        [PreviewInInspector] private List<T> _updateList => _updateSet.ToList();

        public void Register(T updateTarget)
        {
            updateList.Add(updateTarget);
        }

        public void Unregister(T updateTarget)
        {
            toUnregisterUpdateList.Add(updateTarget);
        }

        public void ClearNull()
        {
            _updateSet.RemoveWhere((t) => t == null);
        }

        public void ClearRef()
        {
            _updateSet.Clear();
            updateList.Clear();
            toUnregisterUpdateList.Clear();
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
}