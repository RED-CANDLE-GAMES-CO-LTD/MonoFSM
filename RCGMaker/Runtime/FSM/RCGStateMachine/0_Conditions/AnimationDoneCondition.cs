using RCGFSM.Animation;

namespace RCGMaker.Core
{
    public class AnimationDoneCondition : AbstractConditionComp
    {
        protected override bool isValid => action.IsDone;
        [AutoParent] private AnimatorPlayAction action;
    }
}