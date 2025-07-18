using System;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    //var local ref? 
    public class VarRef : AbstractVariableProviderRef
    {
        [DropDownRef] [SerializeField] private AbstractMonoVariable _monoVariable;

        public override AbstractMonoVariable VarRaw => _monoVariable;
        public override Type GetValueType => _monoVariable?.ValueType;
        public override Type GetVarType => _monoVariable?.GetType();
        public override VariableTag varTag => _monoVariable?._varTag;

        public override TVariable GetVar<TVariable>()
        {
            if (_monoVariable is TVariable variable) return variable;
            throw new InvalidCastException($"Cannot cast {_monoVariable.GetType()} to {typeof(TVariable)}");
        }

        public override T1 Get<T1>()
        {
            return _monoVariable.GetValue<T1>();
        }

        public override Type ValueType => _monoVariable?.ValueType ?? typeof(object);
        public override string Description => _monoVariable != null ? _monoVariable.ToString() : "VarRef is null";
    }
}