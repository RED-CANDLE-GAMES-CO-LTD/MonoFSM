using Gizmo;
using MonoDebugSetting;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class EditorMonoNodeExtension
{
    [Conditional("UNITY_EDITOR")]
    public static void LogErrorUsedCheck(this MonoBehaviour target, string message)
    {
        Debug.LogError("有是缺東西沒綁到還是要關掉/刪掉？" + message, target);
    }

    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }

    public static IEnumerable<Type> FindSubClassesOf(this MonoBehaviour owner, Type type)
    {
        var baseType = type;
        var assembly = baseType.Assembly;
        return assembly.GetTypes().Where(t => t.IsSubclassOf(baseType) || (t == type && t.IsAbstract == false));
    }

    /// <summary>
    /// Debug想要在Scene看到物理判定的位置/形狀
    /// </summary>
    /// <param name="gobj"></param>
    /// <param name="position">在哪裡噴</param>
    /// <param name="name"></param>
    [Conditional("UNITY_EDITOR")]
    public static void
        CreateGizmoDebugNode(this MonoBehaviour gobj, Vector3 position, GameObject name) //TODO: 設圖形?? rect, radius?
    {
        var (isLogging, provider) = MonoExtensionLogger.IsLoggingCheck(gobj);
        if (isLogging == false)
            return;
#if UNITY_EDITOR
        var debugAnchor = new GameObject("[DebugAnchor]:" + name);
        debugAnchor.transform.position = position;
        //TODO: gizmo
        //直接掛gizmo marker
        debugAnchor.AddComponent<GizmoMarker>();
        //TODO: 設圖形??

        // debugAnchor.AddComponent<Gizmo
        // Debug.Break();
        EditorGUIUtility.PingObject(debugAnchor);
#endif
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawLineGizmoNode(this MonoBehaviour mono, string name, Vector3 start, Vector3 end, Color color)
    {
        var provider = mono.GetComponentInParent<DebugProvider>();
        if (provider == null || provider.IsLogInChildren == false)
            return;

        var debugAnchor = new GameObject("[GizmoLine]:" + name);
        debugAnchor.transform.position = start;
        var lineGizmoNode = debugAnchor.AddComponent<LineGizmoNode>();
        lineGizmoNode.offset = end - start;
        lineGizmoNode.color = color;

        // debugAnchor.AddComponent<GizmoMarker>();
        // Debug.DrawLine(start, end, color, 1f);
    }

    /// <summary>
    /// Find all class types that derives from the given <see cref="baseType"/> and:
    /// 1. aren't generic or abstract if the <see cref="baseType"/> is a class.
    /// 2. are <see cref="MonoBehaviour"/>s if the <see cref="baseType"/> is an interface.
    /// If the <see cref="baseType"/> is an interface, the list will only include inherited types that are <see cref="MonoBehaviour"/>.
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    public static List<Type> FilterSubClassOrImplementationFromDomain(this Type baseType)
    {
        var typeList = new List<Type>();
        if (baseType.IsClass && !baseType.IsAbstract && !baseType.IsGenericType)
        {
            // We also want to include the given type if it's not an abstract or a generic type.
            typeList.Add(baseType);
        }

        // if (baseType.IsInterface)
        // {
        //     var allMonoTypes = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();
        //     foreach (var type in allMonoTypes)
        //         // 檢查是否有實作 ICompProvider<T>（T 任何型別）且可 assign 到 ICompProvider<Component>
        //     foreach (var iface in type.GetInterfaces())
        //         if (iface.IsGenericType)
        //             if (baseType.IsAssignableFrom(iface))
        //             {
        //                 typeList.Add(type);
        //                 break;
        //             }
        // }
        
        var types = TypeCache.GetTypesDerivedFrom(baseType);
        if (baseType.IsInterface)
        {
            foreach (Type type in types)
            {
                if (type.InheritsFrom<MonoBehaviour>())
                {
                    typeList.Add(type);
                }
            }
        }
        else // if baseType.IsClass
        {
            foreach (Type type in types)
            {
                if (type.IsClass && !type.IsAbstract && !type.IsGenericType)
                {
                    typeList.Add(type);
                }
            }
        }
        return typeList;
    }
    
    /// <summary>
    /// Find all subclasses of the <see cref="baseType"/> who have the given attribute.
    /// </summary>
    /// <param name="baseType"></param>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public static List<Type> FilterSubClassFromDomain(this Type baseType, Type attribute)
    {
        var typeList = new List<Type>();
        var types = TypeCache.GetTypesDerivedFrom(baseType);
        foreach (Type type in types)
        {
            if (type.IsClass && !type.IsAbstract && !type.IsGenericType && type.GetCustomAttributes(attribute, true).Length > 0)
            {
                typeList.Add(type);
            }
        }

        return typeList;
    }

    public static IEnumerable<Type> GetAllScriptableAssetType()
    {
        //[]: 好像不一定需要這個attribute 才能拿? 但這個是介面問題，不該拿到不能變成asset的SO，但不確定有需要動態產生SO嗎？
        var types = typeof(ScriptableObject).FilterSubClassFromDomain(typeof(CreateAssetMenuAttribute));
        return types;
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void DebugLog(this Component owner, string result)
    {
        if (DebugSetting.IsDebugMode)
            Debug.Log(result, owner);
    }
}
