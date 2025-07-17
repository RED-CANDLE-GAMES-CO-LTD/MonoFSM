using MonoFSM.Core.Attributes;
using UnityEngine;

namespace MonoFSM.Runtime.Mono
{
    //好像也沒有用到
    public class MonoDescriptableFromCollection : MonoBehaviour, IMonoDescriptable
    {
        [PreviewInInspector] [AutoParent] IMonoDescriptableCollection _collection;
        public MonoEntityTag tag;
        public int index;

        //fixme;   
        public MonoEntityTag Key => tag; //_collection?.MonoDescriptableList[index]?.Key;

        [PreviewInInspector]
        public IDescriptableData Descriptable
        {
            get
            {
                if (_collection == null) return null;
                if (_collection.MonoDescriptableList == null) return null;
                if (_collection.MonoDescriptableList.Count <= index) return null;
                return _collection?.MonoDescriptableList[index]?.Descriptable;
            }
        }
    }
}