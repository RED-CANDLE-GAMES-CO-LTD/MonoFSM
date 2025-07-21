using MonoFSM.Core.Runtime.Action;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    public class SetVarFloatConstAction : AbstractStateAction
    {
        [FormerlySerializedAs("targetFlag")]
        // [MCPExtractable] 
        [DropDownRef]
        public VarFloat targetVar;
        public float TargetValue;

        protected override void OnActionExecuteImplement()
        {
            targetVar.SetValue(TargetValue, this);
        }
    }
}