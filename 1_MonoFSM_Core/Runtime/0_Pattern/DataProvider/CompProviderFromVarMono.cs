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

        [FormerlySerializedAs("_varBlackboard")]
        public VarEntity _varEntity; //用Provider?

        public Component Get()
        {
            if (_varEntity == null || _varEntity.Value == null) return null;
            if (_monoTypeData == null)
            {
                Debug.LogError("SystemTypeData is not set on " + gameObject.name, this);
                return null;
            }

            var t = _varEntity.Value["t"];
            return _varEntity.Value.GetComp(_monoTypeData.Type);
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