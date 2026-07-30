using System;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._1_Conditions.Activator
{
    public interface IActivateCheckTarget
    {
        public bool IsValid { get; }
        public GameObject gameObject { get; }
    }

    public class GameObjectActivateRenderGroupCheck : AbstractDescriptionBehaviour
    {
        [ShowInInspector] [AutoChildren] IActivateCheckTarget[] _targets;

        private void LateUpdate()
        {
            foreach (var target in _targets)
            {
                if (target.gameObject.activeSelf != target.IsValid)
                    target.gameObject.SetActive(target.IsValid);
            }
        }
    }
}
