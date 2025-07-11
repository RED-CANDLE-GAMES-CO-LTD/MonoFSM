namespace MonoFSM.Core
{
    //時間到就是true
    public class StateTimeUpCondition : AbstractConditionBehaviour
    {
        [AutoParent] private GeneralState _parentState;
        public float time;
        protected override bool IsValid => _parentState.statusTimer >= time;
    }
}