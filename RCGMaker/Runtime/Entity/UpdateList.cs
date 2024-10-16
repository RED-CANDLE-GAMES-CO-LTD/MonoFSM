using System;
using System.Collections.Generic;
using System.Linq;
using RCGMaker.Core.Attributes;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

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

        //FIXME: 可能同frame Register/Unregister
        public void Register(T updateTarget)
        {
            updateList.Add(updateTarget);
            if (toUnregisterUpdateList.Contains(updateTarget))
            {
                // Debug.LogError("Register same frame", updateTarget.gameObject);
                toUnregisterUpdateList.Remove(updateTarget);
            }
        }

        public void Unregister(T updateTarget)
        {
            // Debug.Log("Unregister", updateTarget.gameObject);
            toUnregisterUpdateList.Add(updateTarget);
            if (updateList.Contains(updateTarget))
            {
                // Debug.LogError("Unregister same frame", updateTarget.gameObject);
                updateList.Remove(updateTarget);
            }
        }

        public void ClearNull()
        {
            //destroyed check, null check is not enough, 

            _updateSet.RemoveWhere((t) => t.IsUnityNull());
            Debug.Break();
        }

        public void ClearRef()
        {
            // _updateSet.Clear();
            // updateList.Clear();
            // toUnregisterUpdateList.Clear();
        }

        public void UpdateManual()
        {
            foreach (var updateTarget in toUnregisterUpdateList)
            {
                _updateSet.Remove(updateTarget);
            }

//FIXME: 順序很重要，先進先出...如果
            //同frame開關？
            foreach (var updateTarget in updateList)
            {
                _updateSet.Add(updateTarget);
            }
            
            toUnregisterUpdateList.Clear();
            updateList.Clear();
            foreach (var updateTarget in _updateSet)
            {
                // Profiler.BeginSample("updateTarget", updateTarget.gameObject);
                _updateAction(updateTarget);
                // Profiler.EndSample();
            }
        }
    }
}