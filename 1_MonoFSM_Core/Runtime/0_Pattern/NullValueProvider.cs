using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core
{
    //null Object Provider?
    public class NullValueProvider : MonoBehaviour, IValueProvider
    {
        public T1 Get<T1>()
        {
            return default;
        }

        public Type ValueType => typeof(Object);
        public string Description => "null";
    }
}