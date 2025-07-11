using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core
{
    //從provider拿到animator
    public class AnimatorPlayingStateCondition : AbstractConditionBehaviour
    {
        //拿動畫上的所有state name
#if UNITY_EDITOR
        public IEnumerable<string> GetAnimatorStateNames()
        {
            return AnimatorHelpler.GetAnimatorStateNames(PreviewTarget, layerIndex);
        }
#endif
        [PreviewInInspector] [AutoParent] private IAnimatorGetter animatorProvider;

        [ShowInPlayMode] private Animator _animator => animatorProvider?.GetAnimator;

        [HideInPlayMode] [FormerlySerializedAs("target")]
        public Animator PreviewTarget;

#if UNITY_EDITOR
        [ValueDropdown(nameof(GetAnimatorStateNames), IsUniqueList = true, NumberOfItemsBeforeEnablingSearch = 3)]
#endif
        public string stateName;

        public int layerIndex = 0;

        protected override bool IsValid
        {
            get
            {
                if (_animator == null)
                    return false;
                return _animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
            }
        }
    }
}