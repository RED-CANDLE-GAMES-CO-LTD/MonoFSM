using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


#if UNITY_EDITOR
public static class AnimatorHelper
{
    public static IEnumerable<string> GetAnimatorStateNames(UnityEngine.Animator animator, int stateLayer)
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

    public static IEnumerable<string> GetAnimatorParameterNames(Animator animator)
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

    public static UnityEditor.Animations.AnimatorController GetAnimatorController(this Animator animator)
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

    public static void EditClip(Animator animator, AnimationClip clip)
    {
        //get animation window
        Selection.activeObject = animator.gameObject;
        var animationWindow = GetAnimationWindow();
        animationWindow.animationClip = clip;
        animationWindow.recording = true;
    }

    static AnimationWindow GetAnimationWindow()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var type = assembly.GetType("UnityEditor.AnimationWindow");
        var window = EditorWindow.GetWindow(type);
        return window as AnimationWindow;
    }
}
#endif