using jerryee.UnityMCP;
using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable;

namespace RCGFSM.Variable
{
    public class SetVariableFloatAction : AbstractStateAction
    {
        [MCPExtractable] [DropDownRef] public VarFloat targetFlag;
        public float TargetValue;

        protected override void OnStateEnterImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}