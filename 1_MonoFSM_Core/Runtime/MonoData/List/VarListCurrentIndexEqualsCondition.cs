using MonoFSM.Core.Variable;
using MonoFSM.Variable;

namespace _1_MonoFSM_Core.Runtime.MonoData.List
{
    public class VarListCurrentIndexEqualsCondition : AbstractConditionBehaviour
    {
        public AbstractVarList _varList;
        public VarIntWrapper _compareIndex;

        protected override bool IsValid =>
            _varList != null && _varList.CurrentIndex == _compareIndex.Value;
    }
}
