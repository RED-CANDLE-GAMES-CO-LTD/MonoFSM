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
            // animDefaultNameHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

            //關掉Animator，原本會清資料，重打開把當下的值當作新的default，會爛掉
            //animator.keepAnimatorStateOnDisable = true; //default state會是原本的 State，和 value沒有關係
            animator.writeDefaultValuesOnDisable = true; //如果加這行，表示關掉的時候會變回default state，就不用那個自己call回去了？
            //但一打開...因為keep state，所以

            //PoolObject需求：打開就整個重置就對了,所以原本沒有call到重置才是對的
            //是什麼需要animator Resetter? 怪物從關門戰召喚出來？
        }
    }

    public bool ResetToDefault() //永遠不可以註解掉！！
    {
        if (animator == null)
            return false;

        if (animator.runtimeAnimatorController == null)
        {
            // Debug.LogError("Animator Resetter: animator.runtimeAnimatorController == null" + animator, animator);
            return false;
        }
        
        animator.enabled = true;
        if (animator.isActiveAndEnabled)
        {
            //HUD靠這個把動畫切回去
            animator.Play(animDefaultNameHash, 0, 0);
            //
            animator.Update(0);
        }
        
        return true;
    }
}