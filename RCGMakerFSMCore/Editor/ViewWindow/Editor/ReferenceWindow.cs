using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

//Prefab中和其他Prefab間的component
namespace Reference
{
//     public class ReferenceWindow : OdinEditorWindow
//     {
//         [MenuItem("Window/RCGMaker/Reference Finder")]
//         public static void Open()
//         {
//             var window = GetWindow<ReferenceWindow>();
//             window.titleContent = new GUIContent("Reference");
//             window.Show();
//         }
//
//         public List<ScriptableObject> ScriptableObjects;
//
//         [FormerlySerializedAs("PrefabRef")] [FormerlySerializedAs("MonoBehaviours")]
//         public List<MonoBehaviour> PrefabRefs;
//
//         [Searchable] [OdinSerialize] [Header("用到哪些Prefab")]
//         public Dictionary<Component, List<Object>> componentToPrefabRefDict =
//             new();
//
//         private static int counter = 0;
//
//         public List<FieldInfo> GetAllFieldsRecursive(MonoBehaviour rootComp, object obj,
//             HashSet<Type> visitedTypes = null)
//         {
//             if (obj == null)
//                 return new List<FieldInfo>();
//             var type = obj.GetType();
//             Debug.Log($"GetAllFieldsRecursive {counter++} {type}", obj as UnityEngine.Object);
//             if (visitedTypes == null)
//             {
//                 visitedTypes = new HashSet<Type>();
//             }
//
//             // If this type has already been visited, return an empty list
//             if (visitedTypes.Contains(type))
//             {
//                 return new List<FieldInfo>();
//             }
//
//             // Add the current type to the list of visited types
//             visitedTypes.Add(type);
//
//             var fields =
//                 new List<FieldInfo>(
//                     type.GetFields(BindingFlags.Public | BindingFlags.Instance));
//             var additionalFields = new List<FieldInfo>();
//
//             foreach (var field in fields)
//             {
//                 if (field == null)
//                     continue;
//                 var fieldValue = field.GetValue(obj);
//                 if (fieldValue == null)
//                     continue;
//                 var fieldType = field.FieldType;
//                 // Debug.Log($"fieldType {fieldType} {field.Name}");
//                 // If the field type is a class (but not string), it's not a system type, and it's marked as serializable
//                 if (field.FieldType.IsSubclassOf(typeof(MonoBehaviour)))
//                 {
//                     // Debug.Log($"MonoBehaviour:{field.FieldType}.{field.Name}");
//
//                     var mono = (MonoBehaviour)fieldValue;
//                     if (PrefabUtility.IsPartOfPrefabAsset(mono))
//                     {
//                         // PrefabRefs.Add(mono);
//                         Debug.Log($"Prefab MonoBehaviour:{mono}.{field.Name}", mono);
//                         if (!componentToPrefabRefDict.ContainsKey(rootComp))
//                             componentToPrefabRefDict.Add(rootComp, new List<Object>());
//                         componentToPrefabRefDict[rootComp].Add(mono);
//                     }
//                 }
//                 else if (field.FieldType.IsSubclassOf(typeof(ScriptableObject)))
//                 {
//                     var mono = (ScriptableObject)fieldValue;
//                     ScriptableObjects.Add(fieldValue as ScriptableObject);
//                     if (!componentToPrefabRefDict.ContainsKey(rootComp))
//                         componentToPrefabRefDict.Add(rootComp, new List<Object>());
//                     componentToPrefabRefDict[rootComp].Add(mono);
//                     // Debug.Log($"ScriptableObject: {comp.name}.{field.Name}");
//                 }
//                 else if (fieldType.IsClass &&
//                          fieldType != typeof(string) &&
//                          !fieldType.FullName.StartsWith("System.") &&
//                          fieldType.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
//                 {
//                     // Recursively call this method and add those fields to the additionalFields list
//                     additionalFields.AddRange(GetAllFieldsRecursive(rootComp, fieldValue, visitedTypes));
//                 }
//             }
//
//             // Add additionalFields to fields after the loop
//             fields.AddRange(additionalFields);
//
//             return fields;
//         }
//
//
//         private void OnSelectionChange()
//         {
//             // counter = 0;
//             PrefabRefs.Clear();
//             ScriptableObjects.Clear();
//             componentToPrefabRefDict.Clear();
//
//             var selected = Selection.activeGameObject;
//             // Debug.Log(selected);
//             //find root of transform
//             if (selected != null)
//             {
//                 GameObject root;
//                 if (PrefabUtility.IsPartOfPrefabInstance(selected))
//                     root = PrefabUtility.GetOutermostPrefabInstanceRoot(selected);
//                 else
//                     root = selected.transform.root.gameObject;
//
//                 // var root = selected.transform.root;
//                 if (root != null)
//                 {
//                     //get all children
//                     var comps = root.GetComponentsInChildren<MonoBehaviour>(true);
// //find all fields of comps which is a reference to Monobehaviour
//                     foreach (var comp in comps)
//                     {
//                         var type = comp.GetType();
//                         var fields = GetAllFieldsRecursive(comp, comp);
//                         // foreach (var field in fields)
//                         // {
//                         //     if (field.FieldType.IsSubclassOf(typeof(MonoBehaviour)))
//                         //     {
//                         //         Debug.Log($"MonoBehaviour:{field.FieldType}.{field.Name}");
//                         //         var value = field.GetValue(comp);
//                         //         if (value != null)
//                         //         {
//                         //             var mono = value as MonoBehaviour;
//                         //             if (PrefabUtility.IsPartOfPrefabAsset(mono))
//                         //             {
//                         //                 PrefabRefs.Add(mono);
//                         //                 Debug.Log($"Prefab MonoBehaviour:{comp.name}.{field.Name}");
//                         //                 if (!componentToPrefabRefDict.ContainsKey(comp))
//                         //                     componentToPrefabRefDict.Add(comp, new List<GameObject>());
//                         //                 componentToPrefabRefDict[comp].Add(mono.gameObject);
//                         //             }
//                         //         }
//                         //     }
//                         //     //scriptable object
//                         //     else if (field.FieldType.IsSubclassOf(typeof(ScriptableObject)))
//                         //     {
//                         //         var value = field.GetValue(comp);
//                         //         if (value != null)
//                         //         {
//                         //             ScriptableObjects.Add(value as ScriptableObject);
//                         //             Debug.Log($"ScriptableObject: {comp.name}.{field.Name}");
//                         //         }
//                         //     }
//                         // }
//                     }
//                     // var prefab = PrefabUtility.GetPrefabAssetType(root.gameObject);
//                     // if (prefab == PrefabAssetType.Regular)
//                     // {
//                     //     var path = AssetDatabase.GetAssetPath(root.gameObject);
//                     //     Debug.Log(path);
//                     // }
//                 }
//             }
//         }
//     }
}