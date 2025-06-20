using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSM.VarRef;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.Condition
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

        protected override bool IsValid => targetVariable.Equals(_sourceValueRef);

        public override string Description =>
            $"{_sourceValueRef} == {_targetVarRef}";
        // targetVariable?.objectValue != null &&
        //                                   _sourceValueRef?.GetValue() ==
        //                                 targetVariable?.objectValue;
    }
}