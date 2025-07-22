using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper;
using MonoFSM.Variable;
using MonoFSM.VarRefOld;

namespace MonoFSM.Core.DataProvider.Condition
{
    public class ValueEqualsCondition : AbstractConditionBehaviour //
    {
        //可能想要比Value vs Vlue, 
        // [Component][PreviewInInspector] IVariableProvider _sourceVariableProvider;
        // [Component][PreviewInInspector] IVariableProvider _targetVariableProvider;
        [AutoChildren] [Component] [PreviewInInspector]
        private SourceValueRef _sourceValueRef;

        [AutoChildren] [Component] [PreviewInInspector]
        private SourceValue2Ref _sourceValue2Ref;

        // AbstractMonoVariable sourceVariable => _sourceValueRef?.VarRaw;

        protected override bool IsValid => _sourceValueRef.objectValue == _sourceValue2Ref.objectValue;

        public override string Description =>
            $"{_sourceValueRef} == {_sourceValue2Ref}";
        // targetVariable?.objectValue != null &&
        //                                   _sourceValueRef?.GetValue() ==
        //                                 targetVariable?.objectValue;
    }
}