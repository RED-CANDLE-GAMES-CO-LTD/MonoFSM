using MonoFSM.Animation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core
{
    public class AnimationPassedRatioCondition : AbstractConditionBehaviour
    {
        [Range(0, 1)]
        public float _exitRatio = 0.75f;

        [Required]
        [CompRef]
        [AutoParent]
        private AnimatorPlayAction _action;

        protected override bool IsValid => _action.IsProgressPassedRatio(_exitRatio);
    }
}
