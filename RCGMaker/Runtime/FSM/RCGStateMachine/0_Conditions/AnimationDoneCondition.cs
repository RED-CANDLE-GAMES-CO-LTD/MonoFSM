using RCGFSM.Animation;

namespace RCGMaker.Core
{
    public class AnimationDoneCondition : AbstractConditionComp
    {
        protected override bool IsValid => action.IsDone;
        [AutoParent] private AnimatorPlayAction action;
    }
}