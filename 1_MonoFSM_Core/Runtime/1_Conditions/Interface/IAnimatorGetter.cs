using UnityEngine;

namespace MonoFSM.Core
{
    public interface IAnimatorGetter
    {
        Animator GetAnimator { get; }
    }
}