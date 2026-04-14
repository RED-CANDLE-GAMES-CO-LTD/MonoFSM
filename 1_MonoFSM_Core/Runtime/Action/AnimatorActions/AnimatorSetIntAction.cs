using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    public class AnimatorSetIntAction : AbstractAnimatorSetValueAction
    {
        protected override AnimatorControllerParameterType ExpectedParamType => AnimatorControllerParameterType.Int;

        public override string Description =>
            $"Set Animator Int [{_parameterName}] = {_intValue}";

        [SerializeField] private VarIntWrapper _intValue;

        protected override void OnActionExecuteImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetInteger(_parameterName, _intValue.Value);
        }
    }
}
