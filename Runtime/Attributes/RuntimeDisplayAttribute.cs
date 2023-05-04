using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    [IncludeMyAttributes]
    [ShowIf("@UnityEngine.Application.isPlaying")]
    [ReadOnly]
    [ShowInInspector]
    // [Conditional("UNITY_EDITOR")]
    public class RuntimeDisplayAttribute : Attribute
    {
    }

    [IncludeMyAttributes]
    [Conditional("UNITY_EDITOR")]
    public class EditorOnlyAttribute : Attribute
    {
    }
}