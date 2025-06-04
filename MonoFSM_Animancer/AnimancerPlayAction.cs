using Animancer;
using MonoFSM_Core.Runtime.Action;
using UnityEngine;

namespace MonoFSM_Animancer
{
    public class AnimancerPlayAction : AbstractStateAction
    {
        [DropDownRef]
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField] private AnimationClip _animation;

        protected override void OnStateEnterImplement()
        {
            _animancer.Play(_animation);
        }
    }
}