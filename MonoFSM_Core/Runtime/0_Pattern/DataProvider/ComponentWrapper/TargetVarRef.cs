using System;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMakerFSM.VarRef
{
    public class TargetVarRef : MonoBehaviour, IVariableProvider
    {
        [Component] [Auto] private AbstractVariableProviderRef _providerRef;

        public AbstractMonoVariable VarRaw => _providerRef.VarRaw;
        public Type GetValueType => _providerRef.GetValueType;

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _providerRef.GetVar<TVariable>();
        }

        public override string ToString()
        {
            _providerRef = GetComponent<AbstractVariableProviderRef>();
            if (_providerRef == null) return "";
            return _providerRef.ToString();
        }
    }
}