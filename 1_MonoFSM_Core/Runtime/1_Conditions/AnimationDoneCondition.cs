using RCGFSM.Animation;
using RCGMaker.Core.Attributes;

namespace RCGMaker.Core
{
    //FIXME: 不該用動畫來決定transition, 時間應該獨立計算，可以cache animation clip的時間
    public class AnimationDoneCondition : AbstractConditionComp
    {
        protected override bool IsValid => _action.IsDone;
        [PreviewInInspector] [AutoParent] private AnimatorPlayAction _action;
    }
}