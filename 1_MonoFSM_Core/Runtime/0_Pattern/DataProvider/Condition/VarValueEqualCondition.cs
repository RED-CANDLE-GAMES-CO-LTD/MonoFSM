using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using RCGMakerFSM.VarRef;
using UnityEngine;

namespace RCGMaker.Core.DataProvider.Condition
{
    //還是Condition要用Is開頭？
    //好像太抽象了，MonoVar Compare就好？ 
    //ex: FloatCompareCondition
    public class VarValueEqualCondition : AbstractConditionComp //
    {
        // [Component][PreviewInInspector] IVariableProvider _sourceVariableProvider;
        // [Component][PreviewInInspector] IVariableProvider _targetVariableProvider;
        [AutoChildren] [Component] [PreviewInInspector]
        private TargetVarRef _targetVarRef;

        [AutoChildren] [Component] [PreviewInInspector]
        private SourceValueRef _sourceValueRef;

        private AbstractMonoVariable targetVariable => _targetVarRef?.VarRaw;
        // AbstractMonoVariable sourceVariable => _sourceValueRef?.VarRaw;

        protected override bool IsValid => false;
        // targetVariable?.objectValue != null &&
        //                                   _sourceValueRef?.GetValue() ==
        //                                 targetVariable?.objectValue;
    }
}