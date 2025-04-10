using MonoFSM.Variable;

namespace RCGFSM.Variable
{
    public class SetVariableFloatAction : AbstractStateAction
    {
        public VarFloat targetFlag;
        public float TargetValue;

        protected override void OnStateEnterImplement()
        {
            targetFlag.SetValue(TargetValue, this);
        }
    }
}