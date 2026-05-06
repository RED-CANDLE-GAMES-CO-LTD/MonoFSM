using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

namespace MonoFSM.Animation
{
    //IRenderAction?
    public class AnimatorSetFloatAction : AbstractAnimatorSetParamAction
    {
        protected override string DescriptionTag => "AnimParam";
        protected override AnimatorControllerParameterType ExpectedParamType => AnimatorControllerParameterType.Float;

        public override bool IsDrawingValueInfo => true;
        public override string ValueInfo => _parameterName + ":" + _floatValue.Value;

        public override string Description =>
            $"Float ${_parameterName} = ${_floatValue}";

        [SerializeField] private VarFloatWrapper _floatValue;
        public float _dampTime = 0.1f;

        public override void OnEnterRenderImplement()
        {
            OnRenderImplement();
        }

        /// <summary>
        /// 本來就要在render時 set了？應該在這裡自己做掉？
        /// </summary>
        public override void OnRenderImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetFloat(_parameterName, _floatValue.Value, _dampTime, Time.deltaTime);
        }


    }
}
