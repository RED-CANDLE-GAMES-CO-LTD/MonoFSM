using Sirenix.OdinInspector.Editor;

namespace MonoFSM.Core
{
    [DrawerPriority(0, 100, 0)]
    public class AutoDrawer : AutoFamilyDrawer<AutoAttribute>
    {
        // The base class (AutoFamilyDrawer) now handles all the functionality that was previously in this class
    }
}