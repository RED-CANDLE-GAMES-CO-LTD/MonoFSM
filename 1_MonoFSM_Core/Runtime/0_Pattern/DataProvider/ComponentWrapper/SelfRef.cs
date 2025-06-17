using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using UnityEngine;

namespace MonoFSM.Ref
{
    //const? IValueProvider如果有varKey?
    public class SelfRef : MonoBehaviour, IValueProvider
    {
        [PreviewInInspector] [AutoParent] private MonoDescriptable _descriptable;

        // [PreviewInInspector]
        public object GetValue()
        {
            return _descriptable;
        }

        public T GetValue<T>()
        {
            if (_descriptable is T value) return value;

            Debug.LogError($"SelfRef: Cannot cast to {typeof(T)}", this);
            return default;
        }

        public string Description => "[Mono]Self";
    }
}