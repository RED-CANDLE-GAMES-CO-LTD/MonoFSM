using System;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

// public interface IReferenceTarget
// {
//     public MonoBehaviour RefOwner { get; set; }
// }

[EditorOnly]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public class DoubleClickLabelAttribute : ShowInInspectorAttribute
{
    public readonly string ActionName;

    public DoubleClickLabelAttribute(string actionName = "")
    {
        ActionName = actionName;
    }

    public bool InvokeOnUndoRedo { get; set; }
}