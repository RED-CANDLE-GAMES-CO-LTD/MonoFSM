using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auto.Utils;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCGMaker.Core
{
    [UsedImplicitly]
    [DrawerPriority(0, 100, 0)]
    public class AutoChildrenDrawer : OdinAttributeDrawer<AutoChildrenAttribute>
    {
        private AutoChildrenAttribute _attribute => Attribute;

        //自動撈？
        //FIXME: 用auto抓資料會導致non-serialized field也被當作dirty
        //FIXME: check for prefabStage dirty
        protected override void Initialize()
        {
            var mb = Property.ParentValues[0] as MonoBehaviour;
            if (mb == null) //不是第一層，可能更深
                return;
            var fieldCompType = Property.ValueEntry.TypeOfValue;

            //FIXME: 不是很好...和runtime的不一樣
            if (fieldCompType.IsArray)
            {
                // var listElementType = AutoUtils.GetElementType(fieldCompType);
                var newArray = _attribute.GetComponentsToReference(mb, mb.gameObject, fieldCompType);
                // PrefabStageUtility.GetCurrentPrefabStage().ClearDirtiness();
                // Debug.Log("objs:" + objs + "objs.count" + objs.Length);
                //array compare

                var originArray = Property.ValueEntry.WeakSmartValue as Array;
                if (originArray == null)
                {
                    if (newArray.Length == 0)
                        return;
                    Debug.Log("Different Value" + newArray.Length, mb);
                    Property.ValueEntry.WeakSmartValue = newArray;
                }

                else if (
                    newArray == null)
                {
                    // Debug.Log("Different Value");
                    Property.ValueEntry.WeakSmartValue = (Array)null;
                }
                else if (originArray.Length != newArray.Length)

                {
                    // Debug.Log("Different Length" + originArray.Length + " " + newArray.Length);
                    Property.ValueEntry.WeakSmartValue = newArray;
                }

                else
                {
                    for (var i = 0; i < originArray.Length; i++)
                    {
                        if (originArray.GetValue(i) != newArray.GetValue(i))
                        {
                            // Debug.Log("Different Value");
                            Property.ValueEntry.WeakSmartValue = newArray;
                            break;
                        }
                    }
                }
            }
            else
            {
                var childRef = Attribute.GetTheSingleComponent(mb, fieldCompType) as Component;
                // field.SetValue(mb, childRef);
                if (childRef != (Component)Property.ValueEntry.WeakSmartValue)
                {
                    var parentComp = Property.ParentValues[0] as Component;
                    // Debug.Log("Different Value" + parentComp + Property.Name, parentComp);
                    Property.ValueEntry.WeakSmartValue = childRef;
                }
            }
            //TODO: single comp;
        }

        // private MonoBehaviour mb => Property.ParentValues[0] as MonoBehaviour;
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
        //     // Debug.Log("Parent Comp:" + mb + ",componentType:" + componentType);
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