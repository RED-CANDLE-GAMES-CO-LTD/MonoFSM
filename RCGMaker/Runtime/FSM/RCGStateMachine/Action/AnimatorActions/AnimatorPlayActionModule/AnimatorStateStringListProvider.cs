using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//提供給動畫用的string list, 但是hash效率比較好，盡量用StateHashValue
public class AnimatorStateStringListProvider : AbstractStringProvider
{
    public Animator animator;
    [ValueDropdown("GetAnimatorStateNames")]//, IsUniqueList = true???
    public List<string> list;

    private List<int> hashList;
    [Required]
    public VariableInt currentIndex;
    public override string StringValue => list[currentIndex.Value];
    public int StateHashValue => hashList[currentIndex.Value];

    private void Awake()
    {
        hashList = new List<int>();
        foreach (var name in list)
        {
            hashList.Add(Animator.StringToHash(name));
        }
    }

    int stateLayer => 0;

    public bool HasCurrentAnimation => currentIndex.Value >= 0 && currentIndex.Value < list.Count;

    //FIXME: 底下內容duplicate code
#if UNITY_EDITOR
    bool IsStateNameNotInAnimator(string name)
    {
        var names = GetAnimatorStateNames();
        if (names == null)
            return true;
        foreach (var _name in names)
        {
            if (_name == name)
                return false;
        }
        return true;
    }

    //拿動畫上的所有state name
    private IEnumerable<string> GetAnimatorStateNames()
    {
        return AnimatorHelpler.GetAnimatorStateNames(animator, stateLayer);
        // var ac =  GetAnimatorController(animator);
        //
        // if (ac == null)
        //     return null;
        //
        // var names = new List<string>();
        // foreach (var state in ac.layers[stateLayer].stateMachine.states)
        // {
        //     names.Add(state.state.name);
        // }
        // return names;
    }
#endif


    // private UnityEditor.Animations.AnimatorController GetAnimatorController(Animator animator)
    // {
    //     if (animator == null)
    //     {
    //         return null;
    //     }
    //
    //     var runTimeAc = animator.runtimeAnimatorController;
    //
    //     if (runTimeAc is AnimatorOverrideController)
    //     {
    //         runTimeAc = (runTimeAc as AnimatorOverrideController).runtimeAnimatorController;
    //     }
    //
    //     var ac = runTimeAc as UnityEditor.Animations.AnimatorController;
    //
    //     return ac;
    // }
}
