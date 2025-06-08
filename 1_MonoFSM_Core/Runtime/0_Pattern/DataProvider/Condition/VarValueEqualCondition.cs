using MonoFSM.Variable;
using RCGMaker.Core.Attributes;
using MonoFSM.VarRef;
using UnityEngine;

namespace RCGMaker.Core.DataProvider.Condition
{
    //ex: FloatCompareCondition
    public class VarValueEqualCondition : AbstractConditionComp //
    {
        // [Component][PreviewInInspector] IVariableProvider _sourceVariableProvider;
        // [Component][PreviewInInspector] IVariableProvider _targetVariableProvider;
        [AutoChildren] [Component] [PreviewInInspector]
        private TargetVarRef _targetVarRef;

        [AutoChildren] [Component] [PreviewInInspector]
        private SourceValueRef _sourceValueRef;

        private AbstractMonoVariable targetVariable => _targetVarRef.VarRaw;
        // AbstractMonoVariable sourceVariable => _sourceValueRef?.VarRaw;

        protected override bool IsValid => _sourceValueRef.GetValue() == targetVariable.objectValue;

        public override string Description =>
            $"{_sourceValueRef} == {_targetVarRef}";
        // targetVariable?.objectValue != null &&
        //                                   _sourceValueRef?.GetValue() ==
        //                                 targetVariable?.objectValue;
    }
}