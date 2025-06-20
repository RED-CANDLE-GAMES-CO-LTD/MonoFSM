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
        [FormerlySerializedAs("_varMono")] public VarBlackboard _varBlackboard;

//用SystemType?
        public T Get()
        {
            if (_varBlackboard == null || _varBlackboard.Value == null) return null;
            var t = _varBlackboard.Value["t"];
            return _varBlackboard.Value.GetComp<T>();
        }

        public object GetValue()
        {
            return Get();
        }

        public Type ValueType => typeof(T);

        public string Description => _varBlackboard != null ? "[VarMono]" + _varBlackboard.name : "No VarMono assigned";
    }

    public class RigidBodyFromVarMono : CompProviderFromVarMono<Rigidbody>
    {
    }
}