using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    public class AnimatorSetBoolAction : AbstractAnimatorSetValueAction
    {
        public override string Description =>
            $"Set Animator Bool [{_parameterName}] = {_boolValue}";

        [SerializeField] private VarBoolWrapper _boolValue;

        protected override void OnActionExecuteImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetBool(_parameterName, _boolValue.Value);
        }
    }
}
