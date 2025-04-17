using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auto.Utils;
using Sirenix.OdinInspector.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core
{
    [DrawerPriority(0, 100, 0)]
    public class AutoDrawer : AutoFamilyDrawer<AutoAttribute>
    {
        // The base class (AutoFamilyDrawer) now handles all the functionality that was previously in this class
    }
}