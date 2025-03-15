using System;
using RCGMaker.Core.DataProvider;
using UnityEngine;

namespace RCGMakerFSM.VarRef
{
    public class TargetVarRef : MonoBehaviour, IVariableProvider
    {
        [Component] [Auto] AbstractVariableProviderRef _providerRef;

        public AbstractMonoVariable VarRaw => _providerRef.VarRaw;
        public Type GetValueType => _providerRef.GetValueType;

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _providerRef.GetVar<TVariable>();
        }
    }
}