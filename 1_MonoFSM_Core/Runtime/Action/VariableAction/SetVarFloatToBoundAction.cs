using jerryee.UnityMCP;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;

namespace MonoFSM_Core.Runtime.Action.VariableAction
{
    public class SetVarFloatToBoundAction : AbstractStateAction
    {
        public enum BoundType
        {
            Min,
            Max
        }

        [MCPExtractable] [DropDownRef] public VarFloat _targetVar;
        public BoundType _boundType;

        protected override void OnStateEnterImplement()
        {
            var value = _boundType == BoundType.Min ? _targetVar.Min : _targetVar.Max;
            _targetVar.SetValue(value, this);
        }
    }
}