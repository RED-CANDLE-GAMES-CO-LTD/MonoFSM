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
    public sealed class AutoParentDrawer : AutoFamilyDrawer<AutoParentAttribute>
    {
    }

    //自動撈？
    public class AutoUtils
    {
        public static bool IsSerialized(InspectorProperty property,object belongObj,out FieldInfo privateField)
        {
            
            bool isSerialized = property.Info.GetAttribute<SerializeField>() != null;
            var propName = property.Info.PropertyName;
            FieldInfo publicField = belongObj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance);
            privateField = belongObj.GetType().GetField(propName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (privateField == null && publicField == null)
            {
                Debug.LogError("No Field Found:"+propName);
            }
                
            bool isPublicOrSerialized = publicField != null || isSerialized;
            return isPublicOrSerialized;
        }
        public static void SetPrivate(FieldInfo field,object belongObj,InspectorProperty property,AutoFamily autoAttribute,MonoBehaviour mb,Type componentType)
        {
            field.SetValue(belongObj,
                property.ValueEntry.TypeOfValue.IsArray
                    ? autoAttribute.GetComponentsToReference(mb, mb.gameObject, componentType)
                    : autoAttribute.GetTheSingleComponent(mb, componentType));
        }
        public static void SetSerialized(IPropertyValueEntry valueEntry,AutoFamily autoAttribute,MonoBehaviour mb,Type componentType)
        {
            valueEntry.WeakSmartValue = valueEntry.TypeOfValue.IsArray
                ? autoAttribute.GetComponentsToReference(mb, mb.gameObject, componentType)
                : autoAttribute.GetTheSingleComponent(mb, componentType);
        }
    }
    
    [DrawerPriority(0, 100, 0)]
    public abstract class AutoFamilyDrawer<TAutoFamily> : OdinAttributeDrawer<TAutoFamily> where TAutoFamily : AutoFamily
    {
        Type componentType => Property.ValueEntry.TypeOfValue;
        object belongObj => Property.ParentValues[0];

        private MonoBehaviour GetMB
        {
            get
            {
                var parent = Property.FindParent((parent) => parent.ParentValues[0] is MonoBehaviour,true);
                if(parent == null)
                    Debug.LogError("No MonoBehaviour Parent Value found?"+Property.Name);
                var belongMb = parent.ParentValues[0] as MonoBehaviour;
                return belongMb;
            }
        }
        

        
        
        //FIXME: 
        
        protected override void Initialize()
        {
            // Debug.Log("AutoFamilyDrawer",mb);
            var mb = GetMB;
            if (mb == null)
            {
                Debug.LogError("No Parent GetMB Value");
                return;
            }
            if(componentType == null)
            {
                Debug.LogError("No Component Type Found");
                return;
            }
            
            //if property is array element
            if (Property.Name.Contains('$'))
            {
                return;
            }

            // Debug.Log("Init: "+componentType+" "+mb+" "+Property.Name);
            var isSerialized = AutoUtils.IsSerialized(Property,belongObj,out var privateField);
            if (isSerialized)
                AutoUtils.SetSerialized(Property.ValueEntry,Attribute,mb,componentType);
            else
                AutoUtils.SetPrivate(privateField,belongObj,Property,Attribute,mb,componentType);
            // var componentType = Property.ValueEntry.TypeOfValue;
            // var currentProperty = Property;
            // //任何
            //
            //
            // // return;
            // if (mb == null) //pure class property //FIXME: 為什麼需要這個？
            // {
            //     // Debug.LogError("No Parent Value");
            //     if (Property.Parent.ParentValues[0] == null)
            //     {
            //         return;
            //     }
            //
            //     // Debug.LogError("No Parent Value?");
            //     var mb = Property.Parent.ParentValues[0] as MonoBehaviour;
            //     var target = Attribute.GetTheSingleComponent(mb, componentType);
            //     //mb.GetComponentInParent(componentType, true);
            //     Property.ValueEntry.WeakSmartValue = target;
            //     return;
            // }
            //
            //
            // //if parent value is array
            // if (Property.ValueEntry.TypeOfValue.IsArray)
            // {
            //     Debug.Log("[AutoParent Drawer] Array:" + Property.ValueEntry.TypeOfValue + mb, mb);
            //     var array = Attribute.GetComponentsToReference(mb, mb.gameObject, componentType);
            //     Property.ValueEntry.WeakSmartValue = array;
            //     return;
            // }
            //
            // // Debug.Log("[AutoParent Drawer] ComponentType?:" + componentType);
            // var targetValue = Attribute.GetTheSingleComponent(mb, componentType);
            //
            // // if (componentType.IsArray)
            // // {
            // //     var listElementType = AutoUtils.GetElementType(componentType);
            // //     var objs = GetComponentsInChildren(listElementType);
            // //     Debug.Log("objs:" + objs + "objs.count" + objs.Length);
            // if (targetValue != Property.ValueEntry.WeakSmartValue)
            //     Property.ValueEntry.WeakSmartValue = targetValue;
            // }

            //TODO: single comp;
        }

        

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