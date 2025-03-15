using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auto.Utils;
using RCGMaker.Runtime.FSM._2_Variable;
using Sirenix.OdinInspector.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core
{
    [DrawerPriority(0, 100, 0)]
    public class AutoParentDrawer : OdinAttributeDrawer<AutoParentAttribute>
    {
        protected override void Initialize()
        {
            var componentType = Property.ValueEntry.TypeOfValue;
            if (mb == null) //pure class property //FIXME: 為什麼需要這個？
            {
                // Debug.LogError("No Parent Value");
                if (Property.Parent.ParentValues[0] == null)
                {
                    return;
                }

                // Debug.LogError("No Parent Value?");
                var mb = Property.Parent.ParentValues[0] as MonoBehaviour;
                var target = Attribute.GetTheSingleComponent(mb, componentType);
                //mb.GetComponentInParent(componentType, true);
                Property.ValueEntry.WeakSmartValue = target;
                return;
            }


            //if parent value is array
            if (Property.ValueEntry.TypeOfValue.IsArray)
            {
                Debug.Log("[AutoParent Drawer] Array:" + Property.ValueEntry.TypeOfValue + mb, mb);
                var array = Attribute.GetComponentsToReference(mb, mb.gameObject, componentType);
                Property.ValueEntry.WeakSmartValue = array;
                return;
            }

            // Debug.Log("[AutoParent Drawer] ComponentType?:" + componentType);
            var targetValue = Attribute.GetTheSingleComponent(mb, componentType);
            
            // if (componentType.IsArray)
            // {
            //     var listElementType = AutoUtils.GetElementType(componentType);
            //     var objs = GetComponentsInChildren(listElementType);
            //     Debug.Log("objs:" + objs + "objs.count" + objs.Length);
            if (targetValue != Property.ValueEntry.WeakSmartValue)
                Property.ValueEntry.WeakSmartValue = targetValue;
            // }

            //TODO: single comp;
        }

        private MonoBehaviour mb => Property.ParentValues[0] as MonoBehaviour;

        // private Array GetComponentsInChildren(Type componentType)
        // {
        //     var mb = Property.ParentValues[0] as MonoBehaviour;
        //
        //     if (Attribute.DepthOneOnly)
        //     {
        //         // var list = new List<Component>();
        //         var all = new List<object>();
        //
        //         var comps = mb.GetComponents(componentType);
        //         all.AddRange(comps);
        //
        //         foreach (Transform t in mb.transform)
        //         {
        //             var result = t.GetComponents(componentType);
        //             all.AddRange(result);
        //         }
        //
        //         var dest = Array.CreateInstance(componentType, all.Count);
        //         Array.Copy(all.ToArray(), dest, all.Count);
        //         return dest;
        //     }
        //
        //     Debug.Log("Parent Comp:" + mb + ",componentType:" + componentType);
        //
        //     var results = mb.GetComponentsInChildren(componentType, true);
        //     var destinationArray = Array.CreateInstance(componentType, results.Length);
        //     Array.Copy(results, destinationArray, results.Length);
        //     return
        //         destinationArray; //Array.ConvertAll(results, item => Convert.ChangeType(item, componentType));
        // }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
        }
        
    }
}