using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace RCGMaker.Core.Attributes
{
    [GUIColor(1, 1, 0, 1)]
    [IncludeMyAttributes]
    [ShowInInspector]
    [ShowIf("@DebugSetting.IsDebugMode")]
    [UsedImplicitly]
    public class ShowInDebugMode : Attribute
    {
        // !DebugSetting.IsDebugMode
    }
}