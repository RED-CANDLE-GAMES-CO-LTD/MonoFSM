using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core.Attributes
{
    [IncludeMyAttributes]
    [HideInPlayMode] //NOTE: 沒用，還是會call property, 但是不會顯示
    [ShowInInspector, ReadOnly]
    [Conditional("UNITY_EDITOR")]
    public class RuntimeDisplayAttribute : Attribute
    {
    }

    [IncludeMyAttributes]
    [Conditional("UNITY_EDITOR")]
    public class EditorOnlyAttribute : Attribute
    {
    }

    [IncludeMyAttributes]
    [BoxGroup("設定")]
    [Conditional("UNITY_EDITOR")]
    public class ConfigAttribute : Attribute
    {
    }
}