using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    public interface IAnimatorGetter
    {
        Animator GetAnimator();
    }

    public class AnimatorPlayingStateCondition : AbstractConditionComp
    {
        //拿動畫上的所有state name
#if UNITY_EDITOR
        public IEnumerable<string> GetAnimatorStateNames()
        {
            return AnimatorHelpler.GetAnimatorStateNames(target, layerIndex);
        }
#endif
        [PreviewInInspector] [AutoParent] private IAnimatorGetter animatorProvider;
        private Animator _animator => animatorProvider?.GetAnimator();
        public Animator target;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetAnimatorStateNames), IsUniqueList = true, NumberOfItemsBeforeEnablingSearch = 3)]
#endif
        public string stateName;

        public int layerIndex = 0;
        protected override bool isValid => _animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
    }
}