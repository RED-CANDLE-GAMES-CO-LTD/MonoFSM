using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    public abstract class AbstractVariableProviderComp : MonoBehaviour
    {
        public GameFlagBase FinalData => VarRaw?.FinalData;
        public abstract AbstractMonoVariable VarRaw { get; } //還是其實這個也可以？
        public abstract Type GetValueType { get; }

        public abstract TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable;

        [Button]
        void Rename()
        {
            name = "[Ref]" + VarRaw?.name;
        }
    }
}