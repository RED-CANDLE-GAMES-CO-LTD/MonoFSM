using System;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using UnityEngine;

namespace RCGMaker.Core.DataProvider.ComponentWrapper
{
    [Serializable]
    public class VariableProviderFromComp : IVariableProvider
    {
        [DropDownRef] public GetMonoComp _getMonoComp;
        public AbstractMonoVariable VarRaw => _getMonoComp?.Variable;
        public Type GetValueType => _getMonoComp?.Variable?.ValueType;

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _getMonoComp?.Variable as TVariable;
        }
    }

    //直接拿到遠距的VarMono
    public class GetMonoComp : MonoBehaviour, IVariableProvider
    {
        [SerializeReference] public IVarMonoProvider _variableProvider;

        public VarMono Variable => _variableProvider?.Variable;

        // public MonoDescriptable Value => Variable?.Value;
        public AbstractMonoVariable VarRaw => Variable;
        public Type GetValueType => typeof(MonoDescriptable);

        public TVariable GetVar<TVariable>() where TVariable : AbstractMonoVariable
        {
            return _variableProvider?.Variable as TVariable;
        }
    }
}