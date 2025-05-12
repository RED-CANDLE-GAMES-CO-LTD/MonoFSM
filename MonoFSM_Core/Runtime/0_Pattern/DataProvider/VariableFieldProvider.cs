using System;
using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.DataProvider
{
    //這什麼意思？只是給某個variable, 不是給他的Object?
    public class VariableFieldValueProvider : AbstractFieldValueProvider
    {
        protected override AbstractMonoVariable ListenToVariable => _variableProviderRef.VarRaw;
        public override Object targetObject => _variableProviderRef?.VarRaw; //可能是null...怎麼處理
        public override Type targetType => _variableProviderRef.GetVarType; //這個不對ㄅ

        // [Required] [InlineField] [PropertyOrder(-1)] [SerializeReference]
        // public IVariableProvider variableProvider;
    }
}