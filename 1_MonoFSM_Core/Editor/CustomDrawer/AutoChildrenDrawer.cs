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

namespace MonoFSM.Core
{
    [UsedImplicitly]
    [DrawerPriority(0, 100, 0)]
    public class AutoChildrenDrawer : AutoFamilyDrawer<AutoChildrenAttribute>
    {
        
    }
    // public class AutoChildrenDrawer : OdinAttributeDrawer<AutoChildrenAttribute>
    // {
    //     Type componentType => Property.ValueEntry.TypeOfValue;
    //     object belongObj => Property.ParentValues[0];
    //     // private AutoChildrenAttribute _attribute => Attribute;
    //
    //     //自動撈？
    //     //FIXME: 用auto抓資料會導致non-serialized field也被當作dirty
    //     //FIXME: check for prefabStage dirty
    //     protected override void Initialize()
    //     {
    //         var mb = Property.ParentValues[0] as MonoBehaviour;
    //         if (mb == null) //不是第一層，可能更深
    //             return;
    //         var dirtyCount = EditorUtility.GetDirtyCount(mb);
    //         Debug.Log("Before Dirty Count:" + dirtyCount, mb);
    //         var fieldCompType = Property.ValueEntry.TypeOfValue;
    //         // var field = mb.GetType().GetField(Property.Name);
    //         // var isPublic = field != null && field.IsPublic;
    //         var serializedObject = new SerializedObject(mb);
    //         var serializedProperty = serializedObject.FindProperty(Property.UnityPropertyPath);
    //         // var serializedProperty = Property.Tree.GetUnityPropertyForPath(Property.UnityPropertyPath);
    //         var isSerializedProperty = serializedProperty != null;
    //         
    //         FieldInfo field = mb.GetType().GetField(Property.Name, BindingFlags.NonPublic | BindingFlags.Instance);
    //
    //         //不用
    //         // var serializeFieldAttribute = Property.Info.GetAttribute<SerializeField>();
    //         // var isSerialized = serializeFieldAttribute != null;
    //         // var IsUnityPropertyOnly = Property.Info.IsUnityPropertyOnly;
    //         // Debug.Log("isSerialized:" + isSerialized + "isPublic:"+isPublic+" "+"isSerializedProperty:"+isSerializedProperty+""+Property.Name, mb);
    //             
    //         //FIXME: 不是很好...和runtime的不一樣
    //         if (fieldCompType.IsArray)
    //         {
    //             // var listElementType = AutoUtils.GetElementType(fieldCompType);
    //             var newArray = Attribute.GetComponentsToReference(mb, mb.gameObject, fieldCompType);
    //
    //             if (isSerializedProperty)
    //             {
    //                 Debug.Log("SerializedProperty:" + Property.Name, mb);
    //                 Property.ValueEntry.WeakSmartValue = (Array)null;
    //             }
    //             else
    //             {
    //                 if (field != null)
    //                 {
    //                     field.SetValue(mb, newArray);
    //                     Debug.Log("Reflection Set Value to: " +field.Name,mb);
    //                 }
    //                 else
    //                 {
    //                     Debug.LogError("Field not found:" + Property.Name, mb);
    //                 }
    //             }
    //             // PrefabStageUtility.GetCurrentPrefabStage().ClearDirtiness();
    //             // Debug.Log("objs:" + objs + "objs.count" + objs.Length);
    //             //array compare
    //             
    //   
    //             
    //             // var originArray = Property.ValueEntry.WeakSmartValue as Array;
    //             // if (originArray == null)
    //             // {
    //             //     if (newArray.Length == 0)
    //             //         return;
    //             //     Debug.Log("Different Value" + Property+ Property.ValueEntry, mb);
    //             //     // serializedProperty.managedReferenceValue = newArray;
    //             //     // serializedObject.ApplyModifiedProperties();
    //             //     // Property.ValueEntry.WeakSmartValue = newArray;
    //             //     
    //             //   
    //             // }
    //             //
    //             // else if (
    //             //     newArray == null)
    //             // {
    //             //     // Debug.Log("Different Value");
    //             //   
    //             // }
    //             // else if (originArray.Length != newArray.Length)
    //             //
    //             // {
    //             //     // Debug.Log("Different Length" + originArray.Length + " " + newArray.Length);
    //             //     Property.ValueEntry.WeakSmartValue = newArray;
    //             // }
    //             //
    //             // else
    //             // {
    //             //     for (var i = 0; i < originArray.Length; i++)
    //             //     {
    //             //         if (originArray.GetValue(i) != newArray.GetValue(i))
    //             //         {
    //             //             // Debug.Log("Different Value");
    //             //             Property.ValueEntry.WeakSmartValue = newArray;
    //             //             break;
    //             //         }
    //             //     }
    //             // }
    //         }
    //         else
    //         {
    //             var childRef = Attribute.GetTheSingleComponent(mb, fieldCompType) as Component;
    //             // field.SetValue(mb, childRef);
    //             if (childRef != (Component)Property.ValueEntry.WeakSmartValue)
    //             {
    //                 var parentComp = Property.ParentValues[0] as Component;
    //                 // Debug.Log("Different Value" + parentComp + Property.Name, parentComp);
    //                 Property.ValueEntry.WeakSmartValue = childRef;
    //             }
    //         }
    //         
    //         var afterDirtyCount = EditorUtility.GetDirtyCount(mb);
    //         Debug.Log("After Dirty Count:" + afterDirtyCount, mb);
    //         //TODO: single comp;
    //         // if(!isSerializedProperty && !isBeforeSetDirty)
    //         // {
    //         //     Debug.Log("Clear Dirty since"+Property.Name+"is not serialized", mb);
    //         //     
    //         //     // Debug.Log("Dirty Count:" + EditorUtility.GetDirtyCount(mb), mb);
    //         // }
    //     }
    //
    //     // private MonoBehaviour mb => Property.ParentValues[0] as MonoBehaviour;
    //     // private Array GetComponentsInChildren(Type componentType)
    //     // {
    //     //     var mb = Property.ParentValues[0] as MonoBehaviour;
    //     //
    //     //     if (Attribute.DepthOneOnly)
    //     //     {
    //     //         // var list = new List<Component>();
    //     //         var all = new List<object>();
    //     //
    //     //         var comps = mb.GetComponents(componentType);
    //     //         all.AddRange(comps);
    //     //
    //     //         foreach (Transform t in mb.transform)
    //     //         {
    //     //             var result = t.GetComponents(componentType);
    //     //             all.AddRange(result);
    //     //         }
    //     //
    //     //         var dest = Array.CreateInstance(componentType, all.Count);
    //     //         Array.Copy(all.ToArray(), dest, all.Count);
    //     //         return dest;
    //     //     }
    //     //
    //     //     // Debug.Log("Parent Comp:" + mb + ",componentType:" + componentType);
    //     //
    //     //     var results = mb.GetComponentsInChildren(componentType, true);
    //     //     var destinationArray = Array.CreateInstance(componentType, results.Length);
    //     //     Array.Copy(results, destinationArray, results.Length);
    //     //     return
    //     //         destinationArray; //Array.ConvertAll(results, item => Convert.ChangeType(item, componentType));
    //     // }
    //
    //     protected override void DrawPropertyLayout(GUIContent label)
    //     {
    //         CallNextDrawer(label);
    //     }
    // }
}