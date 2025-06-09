using System;
using System.Collections.Generic;
using Auto.Utils;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace MonoFSM_Core.Network
{
    //fixme: 還是要中心化註冊？怎麼做比較好？ cal
    public interface ISimulateRunner
    {
    }

    public sealed class UpdateSimulator : MonoBehaviour
    {
        private void Awake()
        {
            _simulatorList.AddRange(_localSimulators);
        }

        public static void RegisterUpdate(IUpdateSimulate target)
        {
            var parent = target.gameObject.GetComponentInParent<UpdateSimulator>();
            if (parent == null)
            {
                Debug.LogError("UpdateSimulator not found for registration. " +
                               "Please ensure the target is a child of an UpdateSimulator GameObject.",
                    target.gameObject);
                return;
            }

            parent._simulatorList.Add(target);
        }

        public static void UnregisterUpdate(IUpdateSimulate target)
        {
            var parent = target.gameObject.GetComponentInParent<UpdateSimulator>();
            if (parent != null)
                parent._simulatorList.Remove(target);
            else
                Debug.LogWarning("UpdateSimulator not found for unregistration.");
        }

        //FIXME: 可能會動態移除
        [PreviewInInspector] [AutoChildren] private IUpdateSimulate[] _localSimulators;

        private readonly HashSet<IUpdateSimulate> _simulatorList = new(); //HashSet?

        [Required] [CompRef] [Auto] private ISimulateRunner _simulateRunner;

        /// <summary>
        /// 需要依照環境決定怎麼simulate
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            if (_simulatorList == null || _simulatorList.Count == 0)
            {
                Debug.LogWarning("No simulators found to simulate.");
                return;
            }

            foreach (var simulator in _simulatorList)
                if (simulator is { isActiveAndEnabled: true })
                    simulator.Simulate(deltaTime);
        }

        public void AfterUpdate()
        {
            if (_simulatorList == null || _simulatorList.Count == 0)
            {
                Debug.LogWarning("No simulators found to simulate in LateUpdate.");
                return;
            }

            foreach (var simulator in _simulatorList)
                if (simulator is { isActiveAndEnabled: true })
                    simulator.AfterUpdate();
                else
                    Debug.LogWarning("A simulator is null and cannot be simulated in LateUpdate.");
        }
    }
}