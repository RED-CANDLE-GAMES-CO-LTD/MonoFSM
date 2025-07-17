using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Runtime.Mono
{
    //直接從notion table讀取？
    //從scriptable collection => DescriptableData array
    //可以純data就好嗎？
    public class MonoDescriptableCollection : MonoBehaviour, IMonoDescriptableCollection
    {
        public MonoEntityTag Key { get; }
        public IList<IMonoDescriptable> MonoDescriptableList => Collection;
        public bool isActiveAndEnabled { get; }

        [PreviewInInspector] [AutoChildren] private MonoEntity[] Collection;
    }
}