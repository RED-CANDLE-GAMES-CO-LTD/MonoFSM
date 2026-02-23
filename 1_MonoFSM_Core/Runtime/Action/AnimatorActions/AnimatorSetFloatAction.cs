using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    public class AnimatorSetFloatAction : AbstractAnimatorSetValueAction
    {
        public override string Description =>
            $"Set Animator Float [{_parameterName}] = {_floatValue}";

        [SerializeField] private VarFloatWrapper _floatValue;

        protected override void OnActionExecuteImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetFloat(_parameterName, _floatValue.Value);
        }
    }
}
