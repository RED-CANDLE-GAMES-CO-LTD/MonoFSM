using System;
using MonoFSM.Core;
using MonoFSM.Runtime.Variable;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM_Physics.Runtime
{
    //從某個VarMono拿到Prefab上的某個Component


    public abstract class CompProviderFromVarMono<T> : MonoBehaviour, ICompProvider<T> where T : Component
    {
        [FormerlySerializedAs("_varBlackboard")] [FormerlySerializedAs("_varMono")]
        public VarEntity _varEntity;

//用SystemType?
        public T Get()
        {
            if (_varEntity == null || _varEntity.Value == null) return null;
            var t = _varEntity.Value["t"];
            return _varEntity.Value.GetComp<T>();
        }

        public object GetValue()
        {
            return Get();
        }

        public Type ValueType => typeof(T);

        public string Description => _varEntity != null ? "[VarMono]" + _varEntity.name : "No VarMono assigned";
    }

    public class RigidBodyFromVarMono : CompProviderFromVarMono<Rigidbody>
    {
    }
}