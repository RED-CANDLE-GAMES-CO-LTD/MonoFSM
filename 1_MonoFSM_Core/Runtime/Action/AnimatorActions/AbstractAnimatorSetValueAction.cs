using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.Action.AnimatorActions;
using MonoDebugSetting;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Animation
{
    public abstract class AbstractAnimatorSetValueAction : AbstractStateAction
    {
        [HideIf(nameof(HasAnimatorSource))]
        [TitleGroup("Animator")]
        [BoxGroup("Animator/Animator")]
        [Required]
        [DropDownRef]
        public Animator _animator;

        [TitleGroup("Animator")]
        [BoxGroup("Animator/Animator")]
        [SerializeField]
        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private AnimatorRefSource _animatorRefSource;


        [HideIf(nameof(HasAnimatorOrRef))]
        [TitleGroup("Animator")] [DropDownRef] [SerializeField]
        private AnimatorRefSource _externalAnimatorRefSource;

        bool HasAnimatorOrRef => _animator != null || _animatorRefSource != null ||
                                 _externalAnimatorRefSource != null;

        [ValidateInput(nameof(ValidateParameterName), "Parameter not found in Animator or type mismatch")]
        [ValueDropdown(nameof(GetParameterNames))]
        public string _parameterName;

        protected abstract AnimatorControllerParameterType ExpectedParamType { get; }

        [TitleGroup("Animator")]
        [PropertyOrder(-1)]
        [ShowInInspector]
        protected Animator Animator => _externalAnimatorRefSource != null
            ? _externalAnimatorRefSource.Value
            :
            _animatorRefSource != null ? _animatorRefSource.Value : _animator;

        private bool HasAnimatorSource =>
            _animatorRefSource != null || _externalAnimatorRefSource != null;

        private bool _hasCheckedParameter;
        private bool _hasParameter;

        private bool ValidateParameterName(string paramName)
        {
            if (string.IsNullOrEmpty(paramName) || Animator == null) return false;
            foreach (var param in Animator.parameters)
            {
                if (param.name == paramName)
                    return param.type == ExpectedParamType;
            }

            return false;
        }

        private IEnumerable<string> GetParameterNames()
        {
            if (Animator == null) yield break;
            foreach (var parameter in Animator.parameters)
            {
                if (parameter.type == ExpectedParamType)
                    yield return parameter.name;
            }
        }

        protected override bool HasError()
        {
            if (!HasParameter())
            {
                _errorMessage = $"Animator does not have parameter '{_parameterName}'";
                return true;
            }

            return base.HasError();
        }

        protected bool HasParameter()
        {
            // if (_hasCheckedParameter)
            //     return _hasParameter;
            //
            // _hasCheckedParameter = true;
            if (Animator == null)
            {
                _errorMessage = "Animator reference is null";
                _hasParameter = false;
                return false;
            }
            foreach (var param in Animator.parameters)
            {
                if (param.name == _parameterName)
                {
                    if (param.type != ExpectedParamType)
                    {
                        _errorMessage = $"Parameter '{_parameterName}' type mismatch: expected {ExpectedParamType}, got {param.type}";
                        _hasParameter = false;
                        return false;
                    }
                    _hasParameter = true;
                    return true;
                }
            }

            _errorMessage = $"Animator does not have parameter '{_parameterName}'";
            _hasParameter = false;
            return false;
        }

        protected bool TryGetAnimator(out Animator animator)
        {
            animator = Animator;
            if (animator == null || !animator.isActiveAndEnabled)
                return false;

#if UNITY_EDITOR
            if (RuntimeDebugSetting.IsDebugMode)
            {
                if (!HasParameter())
                {
                    Debug.LogWarning(
                        $"Animator parameter '{_parameterName}' does not exist on {animator.gameObject.name}",
                        this);
                    return false;
                }
            }
#endif


            return true;
        }

        // protected override void OnRenderImplement()
        // {
        //     OnActionExecuteImplement();
        // }
    }
}
