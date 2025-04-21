using System;

using Sirenix.OdinInspector;

using RCGMaker.Core.Attributes;

[EditorOnly]
[AttributeUsage(AttributeTargets.All)]
public class DoubleClickLabelAttribute : ShowInInspectorAttribute
{
    public readonly string ActionName;

    public DoubleClickLabelAttribute(string actionName = "") 
        => ActionName = actionName;

    public bool InvokeOnUndoRedo { get; set; }
}