using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    //直接從notion table讀取？
    //從scriptable collection => DescriptableData array
    //可以純data就好嗎？
    public class MonoDescriptableCollection:MonoBehaviour,IMonoDescriptableCollection
    {
        public MonoDescriptableTag Key { get; }
        public IList<IMonoDescriptable> MonoDescriptableList => Collection;
        public bool isActiveAndEnabled { get; }

        [PreviewInInspector]
        [AutoChildren]
        private MonoDescriptable[] Collection;
    }
}