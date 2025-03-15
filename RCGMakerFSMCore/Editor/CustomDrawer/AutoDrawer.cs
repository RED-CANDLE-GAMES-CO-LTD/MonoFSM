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
    public class AutoDrawer : OdinAttributeDrawer<AutoAttribute>
    {
        protected override void Initialize()
        {
            if (mb == null) //不是第一層，可能更深
                return;
            var componentType = Property.ValueEntry.TypeOfValue;
            // var targetValue = Attribute.get
            var targetValue = mb.GetComponent(componentType);
            Property.ValueEntry.WeakSmartValue = targetValue;

            //TODO: single comp;
        }

        private MonoBehaviour mb => Property.ParentValues[0] as MonoBehaviour;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
        }
    }
}