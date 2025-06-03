using jerryee.UnityMCP;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using RCGMaker.Core.DataProvider;

namespace MonoFSM_Core.Runtime.Action.VariableAction
{
    public class SetVarFloatAction : AbstractStateAction
    {
        [MCPExtractable] [DropDownRef] public VarFloat _targetVar;
        [CompRef] [Auto] private IFloatProvider _valueProvider;

        protected override void OnStateEnterImplement()
        {
            var value = _valueProvider.GetFloat();
            _targetVar.SetValue(value, this);
        }
    }
}