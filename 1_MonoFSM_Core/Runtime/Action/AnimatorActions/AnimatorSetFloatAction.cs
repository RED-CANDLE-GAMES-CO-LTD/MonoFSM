using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

namespace MonoFSM.Animation
{
    //IRenderAction?
    public class AnimatorSetFloatAction : AbstractAnimatorSetValueAction
    {
        public override string Description =>
            $"Set Animator Float [{_parameterName}] = {_floatValue}";

        [SerializeField] private VarFloatWrapper _floatValue;
        public float _dampTime = 0.1f;

        /// <summary>
        /// 本來就要在render時 set了？應該在這裡自己做掉？
        /// </summary>
        protected override void OnActionExecuteImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            animator.SetFloat(_parameterName, _floatValue.Value, _dampTime, Time.deltaTime);
        }

        protected override void OnRenderImplement()
        {
            if (!TryGetAnimator(out var animator)) return;
            Profiler.BeginSample("AnimatorSetFloatAction SetFloat", this);
            animator.SetFloat(_parameterName, _floatValue.Value, _dampTime, Time.deltaTime);
            Profiler.EndSample();
        }
    }
}
