using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.Action.AnimatorActions;
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

        [TitleGroup("Animator")] [DropDownRef] [SerializeField]
        private AnimatorRefSource _externalAnimatorRefSource;

        [ValueDropdown(nameof(GetParameterNames))]
        public string _parameterName;

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

        private IEnumerable<string> GetParameterNames()
        {
            if (Animator == null) yield break;
            foreach (var parameter in Animator.parameters)
                yield return parameter.name;
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

            if (!HasParameter())
            {
                Debug.LogWarning(
                    $"Animator parameter '{_parameterName}' does not exist on {animator.gameObject.name}",
                    this);
                return false;
            }

            return true;
        }
    }
}
