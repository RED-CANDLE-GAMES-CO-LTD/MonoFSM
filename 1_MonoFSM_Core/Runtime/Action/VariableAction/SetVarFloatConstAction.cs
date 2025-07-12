using jerryee.UnityMCP;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    public class SetVarFloatConstAction : AbstractStateAction
    {
        [FormerlySerializedAs("targetFlag")] [MCPExtractable] [DropDownRef]
        public VarFloat targetVar;
        public float TargetValue;

        protected override void OnStateEnterImplement()
        {
            targetVar.SetValue(TargetValue, this);
        }
    }
}