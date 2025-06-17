using UnityEngine;

namespace MonoFSM.Core
{
    public interface IAnimatorPlayAction
    {
#if UNITY_EDITOR
        public void EditClip();

        //TODO: 有clip和animator就可以直接把實作做掉了
        public AnimationClip Clip { get; }
        public Animator BindAnimator { get; }
#endif
    }

    public interface IAnimatorStateProvider
    {
        public Animator BindAnimator { get; }
        public int StateLayer { get; }
        public string StateName { get; }
    }
}