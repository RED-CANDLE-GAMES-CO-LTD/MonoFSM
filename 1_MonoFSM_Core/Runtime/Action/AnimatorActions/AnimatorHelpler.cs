using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor.Animations;
public static class AnimatorHelpler
{
    /// <summary>
    /// 取得指定 layer 的 stateMachine，若為 synced layer 則回傳 source layer 的 stateMachine
    /// </summary>
    public static AnimatorStateMachine GetStateMachine(AnimatorController ac, int stateLayer)
    {
        var layer = ac.layers[stateLayer];
        return layer.syncedLayerIndex >= 0
            ? ac.layers[layer.syncedLayerIndex].stateMachine
            : layer.stateMachine;
    }

    /// <summary>
    /// 取得指定 state 的 motion，若為 synced layer 則優先取 override motion
    /// </summary>
    public static Motion GetMotion(AnimatorController ac, int stateLayer, AnimatorState state)
    {
        var layer = ac.layers[stateLayer];
        if (layer.syncedLayerIndex >= 0)
        {
            var overrideMotion = layer.GetOverrideMotion(state);
            return overrideMotion != null ? overrideMotion : state.motion;
        }
        return state.motion;
    }

    public static IEnumerable<string> GetAnimatorStateNames(Animator animator, int stateLayer)
    {
        var ac = GetAnimatorController(animator);

        if (ac == null)
            return null;

        var names = new List<string>();
        var stateMachine = GetStateMachine(ac, stateLayer);
        foreach (var state in stateMachine.states)
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
