using System;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.DataProvider
{
    //這什麼意思？只是給某個variable, 不是給他的Object?
    //這個和VarFloatProviderRef好像很像...
    public class FieldOfVarOfVarProvider : AbstractFieldOfVarProvider //不知道什麼型別...
    {
        // [CompRef] [Required] [Auto]
        // protected AbstractVariableProviderRef _variableProviderRef;
        // public override Object targetObject => _variableProviderRef?.VarRaw; //可能是null...怎麼處理
        // public override Type targetType => _variableProviderRef?.GetVarType; //FIXME: 這個不對ㄅ

        
        // [Required] [InlineField] [PropertyOrder(-1)] [SerializeReference]
        // public IVariableProvider variableProvider;
    }
}

//拿Value / 拿Variable (收斂？
//拿FieldValue of Object
//VarOwnerProvider => VarOwner => VarRef(可以跳過自己拿？) => FieldValueOfVarObject

//任何可能變動的數值都用