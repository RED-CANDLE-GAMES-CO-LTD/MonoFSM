using UnityEngine;

namespace RCGMaker.Core.DataProvider.Condition
{
    //還是Condition要用Is開頭？
    public class VarValueEqualCondition : AbstractConditionComp
    {
        [SerializeReference] IVariableProvider _sourceVariableProvider;
        [SerializeReference] IVariableProvider _targetVariableProvider;

        AbstractMonoVariable targetVariable => _targetVariableProvider.VarRaw;
        AbstractMonoVariable sourceVariable => _sourceVariableProvider.VarRaw;

        protected override bool IsValid => targetVariable.objectValue != null &&
                                           sourceVariable.objectValue ==
                                           targetVariable.objectValue;
    }
}