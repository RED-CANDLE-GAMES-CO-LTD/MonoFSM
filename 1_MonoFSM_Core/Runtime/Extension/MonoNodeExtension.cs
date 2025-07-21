using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Gizmo;
using MonoDebugSetting;
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
    public static string GetPath(this Transform tr)
    {
        if (tr.parent == null)
            return tr.name;
        var parent = tr.parent;
        return parent.GetPath() + "/" + tr.name;
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