using MonoFSM.Core.Runtime.Action;

namespace MonoFSM.Variable
{
    public class SetVariableIntAction : AbstractStateAction
    {
        [DropDownRef] public VarInt targetFlag;
        public int TargetValue;

        public override string Description => $"Set {targetFlag.name} to {TargetValue}";

        protected override void OnActionExecuteImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}