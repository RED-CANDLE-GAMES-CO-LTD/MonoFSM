using RCGFSM.Animation;
using RCGMaker.Core.Attributes;

namespace RCGMaker.Core
{
    public class AnimationDoneCondition : AbstractConditionComp
    {
        protected override bool IsValid => _action.IsDone;
        [PreviewInInspector] [AutoParent] private AnimatorPlayAction _action;
    }
}