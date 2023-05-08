using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    /// <summary> 用在Runtime的property上，會在playmode時顯示
    /// <seealso cref="T:RCGMaker.Core.Attributes.Editor.RuntimeDisplayAttributeProcessor" />
    /// </summary>
    [IncludeMyAttributes]
    // [HideInPlayMode] //NOTE: 沒用，還是會call property, 用AttributeProcess處理的
    [ReadOnly]
    [Conditional("UNITY_EDITOR")]
    public class ShowInPlayModeAttribute : Attribute
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