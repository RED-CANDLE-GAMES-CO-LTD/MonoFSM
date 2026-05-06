using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    public class AnimatorSetIntAction : AbstractAnimatorSetParamAction
    {
        protected override AnimatorControllerParameterType ExpectedParamType => AnimatorControllerParameterType.Int;

        public override bool IsDrawingValueInfo => true;
        public override string ValueInfo => _parameterName + ":" + _intValue.Value;

        public override string Description =>
            $"Set Animator Int [{_parameterName}] = {_intValue}";

        [SerializeField] private VarIntWrapper _intValue;


        public override void OnEnterRenderImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetInteger(_parameterName, _intValue.Value);
        }

        public override void OnRenderImplement()
        {
            OnEnterRenderImplement();
        }
    }
}
