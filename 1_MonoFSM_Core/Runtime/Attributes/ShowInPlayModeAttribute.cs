using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Attributes
{
    /// <summary> 用在Runtime的property上，會在playmode時顯示
    /// <seealso cref="T:RCGMaker.Core.Attributes.Editor.ShowInPlayModeAttributeProcessor" />
    /// </summary>
    [IncludeMyAttributes]
    [MeansImplicitUse]
    // [HideInPlayMode] //NOTE: 沒用，還是會call property, 用AttributeProcess處理的
    // [ReadOnly]
    // [DisableIf("@true")]
    // [ShowDrawerChain]
    [DisableInEditorMode]
    [Conditional("UNITY_EDITOR")]
    public class ShowInPlayModeAttribute : Attribute
    {
        public ShowInPlayModeAttribute()
        {
        }

        public ShowInPlayModeAttribute(bool debugModeOnly = false)
        {
            DebugModeOnly = debugModeOnly;
        }

        //DebugMode下才顯示，需要play才會更新
        //[]: 切換DebugMode時，要怎麼樣才能強迫跑process?
        public bool DebugModeOnly = false;
    }
  

  

    [IncludeMyAttributes]
    [BoxGroup("設定")]
    [Conditional("UNITY_EDITOR")]
    public class ConfigGroupAttribute : Attribute
    {
    }

    
}