using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.VarRefOld
{
    public class TargetVarRef : AbstractDescriptionBehaviour, IVariableProvider
    {
        //Assign對象，必定是variable provider
        [Component] [Auto] private AbstractVariableProviderRef _providerRef;

        public AbstractMonoVariable VarRaw => _providerRef?.VarRaw;
        public Type GetValueType => _providerRef?.ValueType;

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _providerRef.GetVar<TVariable>();
        }

        [PreviewInInspector] public override string Description => ToString();
        protected override string DescriptionTag => "Target Var";


        public override string ToString()
        {
            _providerRef = GetComponent<AbstractVariableProviderRef>();
            if (_providerRef == null) return "";
            return _providerRef.ToString();
        }
    }
}