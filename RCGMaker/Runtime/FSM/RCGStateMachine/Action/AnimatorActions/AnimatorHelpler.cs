using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
public static class AnimatorHelpler
{
    public static IEnumerable<string> GetAnimatorStateNames(Animator animator, int stateLayer)
    {
        var ac = GetAnimatorController(animator);

        if (ac == null)
            return null;

        var names = new List<string>();
        foreach (var state in ac.layers[stateLayer].stateMachine.states)
        {
            names.Add(state.state.name);
        }

        return names;
    }

    public static int GetLayerIndex(Animator animator, string layerName)
    {
        var ac = GetAnimatorController(animator);

        if (ac == null)
            return -1;

        for (var i = 0; i < ac.layers.Length; i++)
        {
            if (ac.layers[i].name == layerName)
                return i;
        }

        return -1;
    }

    public static IEnumerable<string> GetLayerNames(Animator animator)
    {
        var ac = GetAnimatorController(animator);

        if (ac == null)
            return null;


        var names = new List<string>();
        foreach (var layer in ac.layers)
        {
            names.Add(layer.name);
        }

        return names;
    }

    public static IEnumerable<string> GetAnimatoParameterNames(Animator animator)
    {
        var ac = GetAnimatorController(animator);

        if (ac == null)
            return null;

        var names = new List<string>();
        foreach (var parameter in ac.parameters)
        {
            names.Add(parameter.name);
        }

        return names;
    }

    private static UnityEditor.Animations.AnimatorController GetAnimatorController(Animator animator)
    {
        if (animator == null)
        {
            return null;
        }

        var runTimeAc = animator.runtimeAnimatorController;

        if (runTimeAc is AnimatorOverrideController)
        {
            runTimeAc = (runTimeAc as AnimatorOverrideController).runtimeAnimatorController;
        }

        var ac = runTimeAc as UnityEditor.Animations.AnimatorController;

        return ac;
    }
}
#endif