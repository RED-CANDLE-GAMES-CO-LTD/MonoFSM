using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.DataProvider
{
    public class VariableFieldValueProvider : AbstractFieldValueProvider
    {
        public override Object targetObject => variableProvider?.GetMonoVariable;

        [InlineField] [PropertyOrder(-1)] [SerializeReference]
        public IVariableProvider variableProvider;
    }
}