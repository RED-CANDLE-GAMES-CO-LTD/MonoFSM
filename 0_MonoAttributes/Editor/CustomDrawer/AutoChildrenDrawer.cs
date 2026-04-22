using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;

namespace MonoFSM.Core
{
    [UsedImplicitly]
    [DrawerPriority(0, 100, 0)]
    public class AutoChildrenDrawer : AutoFamilyDrawer<AutoChildrenAttribute> { }
}
