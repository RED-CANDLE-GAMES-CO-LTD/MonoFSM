using System;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IReferenceTarget
{
    public MonoBehaviour RefOwner { get; set; }
}

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