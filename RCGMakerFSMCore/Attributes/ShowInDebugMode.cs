using System;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    [GUIColor(1, 1, 0, 1)]
    [IncludeMyAttributes]
    [ShowInInspector]
    [ShowIf("@DebugSetting.IsDebugMode")]
    public class ShowInDebugMode : Attribute
    {
        // !DebugSetting.IsDebugMode
    }
}