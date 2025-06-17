using System;

using UnityEngine;

using Sirenix.OdinInspector;

namespace MonoFSM.Core
{
    [Serializable]
    public class ChildrenInterfaceMonoRef<TInterface, TOwner> where TOwner : MonoBehaviour
    {
        [SerializeField] [AutoParent] private TOwner owner;
        public MonoBehaviour[] ValueSources;
        public TInterface[] ValueSourcesTyped => ValueSources as TInterface[];

        [Button]
        private void GetSerializedComps()
        {
            if (owner == null)
                return;
            var results = owner.GetComponentsInChildren<TInterface>(true);
            ValueSources = new MonoBehaviour[results.Length];
            for (var i = 0; i < results.Length; i++)
            {
                ValueSources[i] = results[i] as MonoBehaviour;
            }
        }
    }
}