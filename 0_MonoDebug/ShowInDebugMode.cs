using System;
using JetBrains.Annotations;
using MonoDebugSetting;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Attributes
{
    [GUIColor(1, 1, 0, 1)]
    [IncludeMyAttributes]
    [ShowInInspector]
    [ShowIf("@" + nameof(RuntimeDebugSetting) + "." + nameof(RuntimeDebugSetting.IsDebugMode))]
    // [UsedImplicitly]
    [MeansImplicitUse]
    public class ShowInDebugMode : Attribute
    {
        // !DebugSetting.IsDebugMode
    }

    [GUIColor(1, 1, 0, 1)]
    [ShowIf("@" + nameof(RuntimeDebugSetting) + "." + nameof(RuntimeDebugSetting.IsDebugMode))]
    [MeansImplicitUse]
    [IncludeMyAttributes]
    [ShowInInspector]
    [DisableIf("@true")]
    public class PreviewInDebugMode : Attribute { }
}
