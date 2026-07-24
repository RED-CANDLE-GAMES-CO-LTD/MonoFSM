using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

namespace MonoFSM.Animation
{
    public class AnimatorSetBoolAction : AbstractAnimatorSetParamAction
    {
        protected override AnimatorControllerParameterType ExpectedParamType => AnimatorControllerParameterType.Bool;

        public override bool IsDrawingValueInfo => true;
        public override string ValueInfo => _parameterName + ":" + _boolValue.Value;

        public override string Description =>
            $"Set Animator Bool [{_parameterName}] = {_boolValue}";

        [SerializeField] private VarBoolWrapper _boolValue;

        public override void OnEnterRenderImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetBool(_parameterName, _boolValue.Value);

        }

        public override void OnRenderImplement()
        {
            Profiler.BeginSample("AimatorSetBoolAction.OnRenderImplement");
            OnEnterRenderImplement();
            Profiler.EndSample();
        }


    }
}
