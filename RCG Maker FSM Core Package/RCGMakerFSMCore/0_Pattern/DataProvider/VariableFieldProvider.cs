using System;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.DataProvider
{
    //這什麼意思？只是給某個variable, 不是給他的Object?
    public class VariableFieldValueProvider : AbstractFieldValueProvider
    {
        protected override AbstractMonoVariable ListenToVariable => _variableProvider.VarRaw;
        public override Object targetObject => _variableProvider?.VarRaw;
        public override Type targetType => _variableProvider.GetValueType;

        // [Required] [InlineField] [PropertyOrder(-1)] [SerializeReference]
        // public IVariableProvider variableProvider;
    }
}