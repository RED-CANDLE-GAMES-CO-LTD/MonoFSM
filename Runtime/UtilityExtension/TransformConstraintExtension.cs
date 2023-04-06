using UnityEngine;
using UnityEngine.Animations;


public static class TransformConstraintExtension
{
    public static void AddPosConstraint(this PositionConstraint constraint, Transform target)
    {
        var src = new ConstraintSource
        {
            weight = 1,
            sourceTransform = target
        };
        constraint.AddSource(src);
    }

    //強制scale不要翻面啦
    public static void ForceScaleConstraint1(this Component go, Transform target)
    {
        var scaleConstraint = go.TryGetCompOrAdd<ScaleConstraint>();
        scaleConstraint.locked = true;
        var src = new ConstraintSource();
        src.weight = 1;
        src.sourceTransform = target;
        scaleConstraint.AddSource(src);
        scaleConstraint.constraintActive = true;
    }

    public static void ForcePostiionConstraint(this Component go, Transform target)
    {
        var scaleConstraint = go.TryGetCompOrAdd<PositionConstraint>();
        scaleConstraint.locked = true;
        var src = new ConstraintSource();
        src.weight = 1;
        src.sourceTransform = target;
        scaleConstraint.AddSource(src);
        scaleConstraint.constraintActive = true;
    }
}