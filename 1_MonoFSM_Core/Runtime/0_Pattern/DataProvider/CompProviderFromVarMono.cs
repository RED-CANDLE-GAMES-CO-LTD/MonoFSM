using System;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable.TypeTag;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.DataProvider
{
    public class CompProviderFromVarMono : MonoBehaviour, ICompProvider<Component>
    {
        [FormerlySerializedAs("_systemTypeData")]
        public CompTypeTag _monoTypeData; //沒有相容關係...

        public VarBlackboard _varBlackboard; //用Provider?

        public Component Get()
        {
            if (_varBlackboard == null || _varBlackboard.Value == null) return null;
            if (_monoTypeData == null)
            {
                Debug.LogError("SystemTypeData is not set on " + gameObject.name, this);
                return null;
            }

            var t = _varBlackboard.Value["t"];
            return _varBlackboard.Value.GetComp(_monoTypeData.Type);
        }

        public object GetValue()
        {
            return Get();
        }

        public T GetValue<T>()
        {
            if (typeof(T) != typeof(Component))
                throw new InvalidOperationException("GetValue<T>() can only be used with Component type.");

            return (T)(object)Get();
        }

        public Type ValueType => typeof(Component);
        public string Description { get; }
    }
}