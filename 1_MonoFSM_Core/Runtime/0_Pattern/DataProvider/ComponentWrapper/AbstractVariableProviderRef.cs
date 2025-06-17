using System;
using MonoFSM.Variable;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public abstract class AbstractVariableProviderRef : MonoBehaviour
    {
        public GameFlagBase FinalData => VarRaw?.FinalData;
        public abstract AbstractMonoVariable VarRaw { get; } //還是其實這個也可以？
        public abstract Type GetValueType { get; }
        public abstract Type GetVarType { get; }
        public abstract VariableTag varTag { get; set; }
        public abstract TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable;

        // [Button]
        // private void Rename()
        // {
        //     name = "[Ref]" + VarRaw?.name;
        // }

        public override string ToString()
        {
            return VarRaw?.name;
        }

        // public virtual string Description
        // {
        //     get
        //     {
        //         if (VarRaw == null) return "Null";
        //         return $"{VarRaw.name}";
        //     }
        // }
    }
}