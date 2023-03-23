using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    [IncludeMyAttributes]
    [ReadOnly]
    [ShowInInspector]
    [ShowIf("@UnityEngine.Application.isPlaying")]
    [Conditional("UNITY_EDITOR")]
    public class RuntimeDisplayAttribute : Attribute
    {
    }
}