using UnityEngine;

namespace RCGMaker.Core.DataProvider.Condition
{
    public class VariableValueCompareCondition : AbstractConditionComp
    {
        [SerializeReference] IVariableProvider _sourceVariableProvider;
        [SerializeReference] IVariableProvider _targetVariableProvider;

        protected override bool isValid => _targetVariableProvider.Variable.objectValue != null &&
                                           _sourceVariableProvider.Variable.objectValue ==
                                           _targetVariableProvider.Variable.objectValue;
    }
}