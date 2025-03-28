using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Gizmo;
using RCGSetting;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
#endif


// public static class ZString
// {
//     
//     public static string Concat(params object[] items)
//     {
//         return string.Join(",", items);
//     }
//
//     public static string Join(string separator, params object[] items)
//     {
//         return string.Join(separator, items);
//     }
// }

// public class RCGLogger : ILogger<MonoBehaviour>
// {
//     public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
//         Func<TState, Exception, string> formatter)
//     {
//         // switch ()
//         // {
//         //     
//         // }
//     }
//
//     public bool IsEnabled(LogLevel logLevel)
//     {
// #if UNITY_EDITOR
//         return true;
// #else
//         return false;
// #endif
//     }
//
//     public IDisposable BeginScope<TState>(TState state)
//     {
//         throw new NotImplementedException();
//     }
// }
public static class MonoNodeExtension
{
    // private static ILogger<DebugProvider> logger;
    // [Conditional("UNITY_EDITOR")]
    // public static void DebugLog(this MonoBehaviour mono, string message)
    // {
    //     Debug.Log(message, mono);
    // }
    // public static ILogger<DebugProvider> Logger(this MonoBehaviour comp)
    // {
    //     LazyInit();
    //     return logger;
    // }
    //
    // // public static ILogger<DebugProvider> L(this MonoBehaviour comp)
    // // {
    // //     //任何class都要有一個logger?
    // // }
    //
    // private static void LazyInit()
    // {
    //     if (logger == null)
    //     {
    //         logger = LoggerFactory.Create(builder =>
    //         {
    //             builder.ClearProviders();
    //             builder.SetMinimumLevel(LogLevel.Debug);
    //             // builder.AddZLoggerConsole();
    //             builder.AddZLoggerUnityDebug();
    //         }).CreateLogger<DebugProvider>();
    //     }
    //
    //     if (logger.IsEnabled(LogLevel.Debug))
    //         logger.LogDebug("Logger Init");
    // }
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


    public static async UniTaskVoid LogException(this Component go, string e)
    {
        // Debug.LogError(e + go.gameObject.name);
        await UniTask.Yield();
        var debugProvider = go.GetComponentInParent<DebugProvider>(true);
        var scene = go.gameObject.scene;
        if (debugProvider != null)
            throw new Exception(e + go.gameObject.name + ",debugProvider:" + debugProvider.gameObject.name + ",at:" +
                                scene.name);
        else
        {
            throw new Exception(e + go.gameObject.name + ",parent:" + CombineAllTransformParentName(go, "") + ",at:" +
                                scene.name);
        }
    }

    private static string CombineAllTransformParentName(this Component go, string message)
    {
        var result = message;
        var parent = go.transform.parent;
        while (parent != null)
        {
            result = ZString.Concat(result, ">", parent.name);
            parent = parent.parent;
        }

        return result;
    }

    [Conditional("UNITY_EDITOR")]
    public static void DebugLog(this Component owner, string result)
    {
        if (DebugSetting.IsDebugMode)
            Debug.Log(result, owner);
    }


    public static T GetComponentInChildrenOfDepthOne<T>(this Component go)
    {
        foreach (Transform child in go.transform)
            if (child.TryGetComponent<T>(out var comp))
                return comp;
        return default;
    }

    public static List<T> GetComponentsInChildrenOfDepthOne<T>(this Component go)
    {
        var list = new List<T>();
        foreach (Transform child in go.transform)
            if (child.TryGetComponent<T>(out var comp))
                list.Add(comp);
        return list;
    }

    public static T TryGetComp<T>(this Component go) //where T : Component
    {
        if (go.TryGetComponent<T>(out var comp)) return comp;
        return default;
    }

    public static T TryGetComp<T>(this GameObject go) //where T : Component
    {
        if (go.TryGetComponent<T>(out var comp)) return comp;
        return default;
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
#if UNITY_EDITOR
        var typeList = new List<Type>();
        if (baseType.IsClass && !baseType.IsAbstract && !baseType.IsGenericType)
        {
            // We also want to include the given type if it's not an abstract or a generic type.
            typeList.Add(baseType);
        }
        
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
#else
        var typeList = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                //有這個interface加進來
                if (baseType.IsInterface && type.GetInterfaces().Contains(baseType) && type.InheritsFrom(typeof(MonoBehaviour)))
                {
                    typeList.Add(type);
                    continue;
                }

                //是class, 不是abstract, 不是泛型, 是t的子類或是t
                if (type.IsClass && !type.IsAbstract && !type.IsGenericType &&
                    (type.IsSubclassOf(baseType) || type == baseType))
                {
                    typeList.Add(type);
                }
            }
        }

        return typeList;
#endif
    }
    
    //Filter sub classes of Type t having certain attribute 
    public static List<Type> FilterSubClassFromDomain(this Type t, Type attri)
    {
        var typeList = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var type in assembly.GetTypes())
            if (type.IsClass && !type.IsAbstract && !type.IsGenericType &&
                type.IsSubclassOf(t) && type.GetCustomAttributes(attri, true).Length > 0)
                typeList.Add(type);

        return typeList;
    }

    public static IEnumerable<Type> GetAllScriptableAsset()
    {
        //[]: 好像不一定需要這個attribute 才能拿? 但這個是介面問題，不該拿到不能變成asset的SO，但不確定有需要動態產生SO嗎？
        var types = typeof(ScriptableObject).FilterSubClassFromDomain(typeof(CreateAssetMenuAttribute));
        return types;
    }

    //這個還是舊規
    [Conditional("UNITY_EDITOR")]
    public static void
        LogWarning(this Component go, object message, UnityEngine.Object context = null) //where T : Component
    {
#if RCG_DEV
        var provider = go.GetComponentInParent<DebugProvider>(true);
        if (provider && provider.IsLogInChildren)
            Debug.LogWarning(ZString.Concat("[", provider.gameObject.name, go.GetInstanceID(), "]\n", message),
                context ?? go.gameObject);
        else if (provider == null)
            Debug.LogWarning(message, context ?? go.gameObject);
#endif
    }

    public static bool IsNull(this Object obj)
    {
        return ReferenceEquals(obj, null);
    }

    public static void RemoveAllNull<T>(this List<T> list) where T : class
    {
#if UNITY_EDITOR
        if (list == null)
        {
            Debug.LogError("list == null");
            return;
        }

        if (NullPredicate == null)
            Debug.LogError("NullPredicate == null");
#else
            if (list == null)
                return;
#endif
        if (NullPredicate != null) list.RemoveAll(NullPredicate);
    }

    /// <summary>
    /// 假設一個object為UnityEngine.Object，然後判斷其是否為unity nuLl而不只C# nULL。
    /// 主要給interface使用：
    /// 當我們對一個實作了某intenface的Unity Object檢查其是否已經被destroy時，/1/ 不能直接用 == nULL，因為它會是ReferenceEquals（）而非UnityEngine.Object.Equals（）的判斷。
    /// 會導致dummy nuLl object被判定成non nULL。
    /// </summary>
    /// ‹param name="unityObject"></param>
    /// returns></returns>
    public static bool IsUnityNull(this object unityObject)
    {
        if (ReferenceEquals(unityObject, null))
        {
            return true;
        }

        var asUnityObject = unityObject as Object;
        return !asUnityObject;
    }

    private static readonly Predicate<object> NullPredicate = (item) => item == null;

    // [Conditional("UNITY_EDITOR")]
    // public static void Log(this Component go, params object[] items)
    // {
    //TODO: 從taopunk弄過來
// #if UNITY_EDITOR
//
//             var (isLogging, providerName) = IsLoggingCheck(go);
//
//             if (isLogging)
//             {
//                 // var fullStr = string.Join(",", items);
//                 var result = ZString.Concat("[", providerName, "]\n", s1, s2, s3, s4);
//                 Debug.Log(result, go);
//                 // UnityEngine.Debug.Log("[" + providerName + "]\n" + fullStr, context);
//             }
// #endif
    // }


    public static T AddChildrenComponent<T>(this GameObject go, string name) where T : MonoBehaviour
    {
        var newGo = new GameObject(name);
#if UNITY_EDITOR
        Selection.activeGameObject = newGo;
        Undo.RegisterCreatedObjectUndo(newGo, "Add Children Component" + typeof(T).Name);
        Undo.SetTransformParent(newGo.transform, go.transform, "Set Parent");
#else
            newGo.transform.SetParent(go.transform);
#endif
        newGo.transform.localPosition = Vector3.zero;

        var comp = newGo.AddComponent(typeof(T)) as T;
        return comp;
    }

    public static GameObject AddChildrenGameObject(this GameObject go, string name)
    {
        var newGo = new GameObject(name);
#if UNITY_EDITOR
        Selection.activeGameObject = newGo;
        Undo.RegisterCreatedObjectUndo(newGo, "Add Children");
        Undo.SetTransformParent(newGo.transform, go.transform, "Set Parent");
#else
            newGo.transform.SetParent(go.transform);
#endif
        newGo.transform.localPosition = Vector3.zero;

        return newGo;
    }

    // public static TBase AddChildrenComponent<TBase>(this MonoBehaviour mono, Type type, string name)
    //     where TBase : MonoBehaviour
    // {
    //     return mono.gameObject.AddChildrenComponent(type, name) as TBase;
    // }
    //

    public static T AddChildrenComponent<T>(this MonoBehaviour go, string name, bool active = true)
        where T : MonoBehaviour
    {
        var newGo = go.gameObject.AddChildrenComponent<T>(name);
        if (active == false)
            newGo.gameObject.SetActive(false);
        // var newGo = new GameObject(name);
        //
        // Undo.RegisterCreatedObjectUndo(newGo, "Add Children Component" + typeof(T).Name);
        // var comp = newGo.AddComponent(typeof(T)) as T;
        // // Undo.IncrementCurrentGroup();
        // // Undo.RecordObject(go.transform, "Transform set Parent");
        // Undo.SetTransformParent(newGo.transform, go.transform, "Set Parent");
        // newGo.transform.localPosition = Vector3.zero;

        // Selection.activeGameObject = newGo;

        return newGo;
    }

    public static Component
        AddChildrenComponent(this GameObject go, Type type, string name)
    {
        var newGo = new GameObject(name);
#if UNITY_EDITOR
        Selection.activeGameObject = newGo;
        Undo.RegisterCreatedObjectUndo(newGo, "Add Children Component" + type.Name);
        Undo.SetTransformParent(newGo.transform, go.transform, "Set Parent");
        var comp = Undo.AddComponent(newGo, type);
#else
            newGo.transform.SetParent(go.transform);
            var comp = newGo.AddComponent(type);
#endif
        newGo.transform.localPosition = Vector3.zero;

        return comp;
    }

    public static T TryGetCompOrAdd<T>(this Component go) where T : Component
    {
        if (go.TryGetComponent<T>(out var comp))
        {
            return comp;
        }
        else
        {
#if UNITY_EDITOR
            return Undo.AddComponent<T>(go.gameObject);
#else
            return go.gameObject.AddComponent<T>();
#endif
        }

        // return default(T);
    }

    public static T TryGetCompOrAdd<T>(this GameObject go) where T : Component
    {
        if (go.TryGetComponent<T>(out var comp))
        {
            return comp;
        }
        else
        {
#if UNITY_EDITOR
            return Undo.AddComponent<T>(go.gameObject);
#else
            return go.gameObject.AddComponent<T>();
#endif
        }

        // return default(T);
    }

    public static Component AddComp(this Component go, Type t)
    {
#if UNITY_EDITOR
        return Undo.AddComponent(go.gameObject, t);

#else
            return go.gameObject.AddComponent(t);
#endif
    }

    // return default(T);


    [Conditional("UNITY_EDITOR")]
    public static void Break(this Component go)
    {
#if UNITY_EDITOR
        var (isLogging, provider) = MonoExtensionLogger.IsLoggingCheck(go);
        // var provider = go.GetComponentInParent<DebugProvider>(true);
        if (isLogging == false)
            return;
        if (provider.IsBreak)
        {
            Debug.Log("[DebugProvider] Break",go);
            Debug.Break();    
        }
#endif
    }
}