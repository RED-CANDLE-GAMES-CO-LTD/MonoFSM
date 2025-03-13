using UnityEngine;

public interface IAnimatorProvider
{
    public Animator ChildAnimator { get; }
    public Animator[] ChildAnimators { get; }
}

public class AnimatorProvider : MonoBehaviour, IAnimatorProvider
{
    [Auto] public Animator animator;

    private void OnValidate()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public Animator ChildAnimator => animator;
    public Animator[] ChildAnimators => animators;
    [AutoChildren] public Animator[] animators;
}