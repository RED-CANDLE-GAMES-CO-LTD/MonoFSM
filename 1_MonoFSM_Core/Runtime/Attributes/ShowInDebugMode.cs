using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Attributes
{
    [GUIColor(1, 1, 0, 1)]
    [IncludeMyAttributes]
    [ShowInInspector]
    [ShowIf("@DebugSetting.IsDebugMode")]
    // [UsedImplicitly]
    [MeansImplicitUse]
    public class ShowInDebugMode : Attribute
    {
        // !DebugSetting.IsDebugMode
    }

    [GUIColor(1, 1, 0, 1)]
    [ShowIf("@DebugSetting.IsDebugMode")]
    [MeansImplicitUse]
    [IncludeMyAttributes]
    [ShowInInspector]
    [DisableIf("@true")]
    public class PreviewInDebugMode : Attribute
    {
    }
}