using jerryee.UnityMCP;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;

namespace MonoFSM.Variable
{
    public class SetVarFloatConstAction : AbstractStateAction
    {
        [MCPExtractable] [DropDownRef] public VarFloat targetFlag;
        public float TargetValue;

        protected override void OnStateEnterImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}