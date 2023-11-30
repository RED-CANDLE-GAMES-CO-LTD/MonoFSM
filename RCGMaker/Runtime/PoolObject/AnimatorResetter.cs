//runtime data

using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class AnimatorResetter
{
    [ShowInInspector] private int animDefaultNameHash;


    public Animator animator;

    public AnimatorResetter(Animator anim)
    {
        animator = anim;
        Fetch();
    }

    public void Fetch()
    {
        if (animator != null && animator.runtimeAnimatorController != null) // && _anim.isActiveAndEnabled)
        {
            animDefaultNameHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

            //關掉Animator，原本會清資料，重打開把當下的值當作新的default，會爛掉
            animator.keepAnimatorStateOnDisable = true;
        }
    }

    public bool ResetToDefault()
    {
        if (animator == null)
            return false;
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator Resetter: animator.runtimeAnimatorController == null" + animator, animator);
            return false;
        }


        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator Resetter: animator.runtimeAnimatorController == null" + animator, animator);
            return false;
        }


        // if ()
        // {
        //     Debug.LogError("Animator Resetter: animator.isActiveAndEnabled == false" + animator, animator);
        // }
        animator.enabled = true;
        Debug.Log("Animator Resetter: Resetting:" + animator, animator);
        animator.Play(animDefaultNameHash, 0, 0);
        animator.Update(0);
        return true;
    }
}