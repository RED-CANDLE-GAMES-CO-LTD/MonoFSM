using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCGMaker.Core
{
    public interface IProxyUpdate
    {
        void UpdateProxy();
    }

    public interface IProxyLateUpdate
    {
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


        private void Awake()
        {
        }

        private void Update()
        {
            foreach (var updateTarget in updateList)
            {
                updateTarget.UpdateProxy();
            }
        }

        private void LateUpdate()
        {
            foreach (var lateUpdateTarget in lateUpdateList)
            {
                lateUpdateTarget.LateUpdateProxy();
            }
        }

        public void ClearReference()
        {
        }
    }
}