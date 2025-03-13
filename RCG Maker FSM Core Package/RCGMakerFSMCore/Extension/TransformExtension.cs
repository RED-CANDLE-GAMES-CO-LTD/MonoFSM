using System;
using UnityEngine;

public static class TransformExtension
{
    /// <summary>
    /// 因為Unity的Transform.right是不會考慮到scale的，所以這個方法會考慮到scale
    /// 2D遊戲用
    public static Vector3 right2D(this Transform transform)
    {
        return transform.right * Math.Sign(transform.lossyScale.x);
    }
}