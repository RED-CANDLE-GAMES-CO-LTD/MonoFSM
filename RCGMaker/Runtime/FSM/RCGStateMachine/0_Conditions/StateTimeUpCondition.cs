namespace RCGMaker.Core
{
    //時間到就是true
    public class StateTimeUpCondition:AbstractConditionComp
    {
        [AutoParent] GeneralState parentState;
        public float time;
        protected override bool isValid => parentState.statusTimer >= time;
    }
}