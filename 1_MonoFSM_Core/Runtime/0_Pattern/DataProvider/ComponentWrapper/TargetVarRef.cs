using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.Core.DataProvider;
using UnityEngine;

namespace MonoFSM.VarRef
{
    public class TargetVarRef : MonoBehaviour, IVariableProvider
    {
        //Assign對象，必定是variable provider
        [Component] [Auto] private AbstractVariableProviderRef _providerRef;

        public AbstractMonoVariable VarRaw => _providerRef?.VarRaw;
        public Type GetValueType => _providerRef?.GetValueType;

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _providerRef.GetVar<TVariable>();
        }

        [PreviewInInspector] public string Description => ToString();
        
       
        public override string ToString()
        {
            _providerRef = GetComponent<AbstractVariableProviderRef>();
            if (_providerRef == null) return "";
            return _providerRef.ToString();
        }
    }
}