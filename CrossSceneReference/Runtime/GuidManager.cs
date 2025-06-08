using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Object = UnityEngine.Object;

public static class GuidExtension
{
    public static GuidComponent GetGuidComponent(this GameObject go)
    {
        return go.GetComponent<GuidComponent>();
    }

    public static GuidComponent GetGuidComponent(this Component go)
    {
        if (go == null)
            return null;
        return go.GetComponent<GuidComponent>();
    }

    public interface IInterface
    {
    }


#if UNITY_EDITOR
    public static GuidComponent TryGetOrAddGuidComponent(this Component go)
    {
        var guidComp = go.GetComponent<GuidComponent>();
        if (guidComp == null)
        {
            Undo.AddComponent<GuidComponent>(go.gameObject);
        }

        return guidComp;
    }
#endif
}
// Class to handle registering and accessing objects by GUID
public class GuidManager
{
    // for each GUID we need to know the Game Object it references
    // and an event to store all the callbacks that need to know when it is destroyed
    private struct GuidInfo
    {
        public GameObject go;
        public event Action<GameObject> OnAdd;
        public event Action OnRemove;

        public GuidInfo(GuidComponent comp)
        {
            go = comp.gameObject;
            OnRemove = null;
            OnAdd = null;
        }

        public void HandleAddCallback()
        {
            if (OnAdd != null)
            {
                OnAdd(go);
            }
        }

        public void HandleRemoveCallback()
        {
            if (OnRemove != null)
            {
                OnRemove();
            }
        }
    }
    


    // Singleton interface
    public static GuidManager Instance;

    //FIXME: 以前有注解掉，
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void Init()
    {
        // Instance = new GuidManager();
        // EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
        // {
        //     if (state == PlayModeStateChange.EnteredPlayMode)
        //     {
        //     }
        // };
    }
#endif

    public static void InitRuntime() //before all scene awake?
    {
        if (Instance == null)
            Instance = new GuidManager();
        Instance.guidToObjectMapRuntime.Clear();
        var guidComps = Object.FindObjectsByType<GuidComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("GuidManager Find All Runtime GuidComponent Count:" + guidComps.Length);

        //排除...
        foreach (var guidComp in guidComps)
        {
            // Debug.Log(guidComp.Guid, guidComp);
            Instance.RuntimeInternalAdd(guidComp);
        }
    }

    // All the public API is static so you need not worry about creating an instance
    public static bool Add(GuidComponent guidComponent )
    {
        if (Instance == null)
        {
            Instance = new GuidManager();
        }
        return Instance.InternalAdd(guidComponent);
    }

    // public static bool AddRuntime(GuidComponent guidComponent)
    // {
    //     return Instance.RuntimeInternalAdd(guidComponent);
    // }

    private bool RuntimeInternalAdd(GuidComponent guidComponent)
    {
        //FIXME:
        if (Instance == null)
        {
            Instance = new GuidManager();
        }
        var guid = guidComponent.Guid;
        
        if (!guidToObjectMapRuntime.ContainsKey(guid))
        {
            var info = new GuidInfo(guidComponent);
            guidToObjectMapRuntime.Add(guid, info);
            return true;
        }

        // var existingInfo = guidToObjectMapRuntime[guid];
        // Debug.Log(
        return false;
    }

    public static void Remove(System.Guid guid)
    {
        Instance.InternalRemove(guid);
    }
    public static GameObject ResolveGuid(System.Guid guid, Action<GameObject> onAddCallback, Action onRemoveCallback)
    {
        return Instance.ResolveGuidInternal(guid, onAddCallback, onRemoveCallback);
    }

    public static GameObject ResolveGuid(System.Guid guid, Action onDestroyCallback)
    {
        if (Instance == null)
            Instance = new GuidManager();
        return Instance.ResolveGuidInternal(guid, null, onDestroyCallback);
    }

    public static GameObject ResolveGuid(System.Guid guid)
    {
        if (Instance == null)
            Instance = new GuidManager();
        return Instance.ResolveGuidInternal(guid, null, null);
    }
        
    // instance data
    private Dictionary<Guid, GuidInfo> guidToObjectMap = new();
    private Dictionary<Guid, GuidInfo> guidToObjectMapRuntime = new();

    private GuidManager()
    {
       
    }

    private bool InternalAdd(GuidComponent guidComponent)
    {
        Guid guid = guidComponent.GetGuid();

        GuidInfo info = new GuidInfo(guidComponent);

        if (!guidToObjectMap.ContainsKey(guid))
        {
            guidToObjectMap.Add(guid, info);
            return true;
        }

        GuidInfo existingInfo = guidToObjectMap[guid];
        if ( existingInfo.go != null && existingInfo.go != guidComponent.gameObject )
        {
            // normally, a duplicate GUID is a big problem, means you won't necessarily be referencing what you expect
            if (Application.isPlaying)
            {
                //先不管吧，重複load同一個景
                // Debug.AssertFormat(false, guidComponent, "Guid Collision Detected between {0} and {1}.\nAssigning new Guid. Consider tracking runtime instances using a direct reference or other method.", (guidToObjectMap[guid].go != null ? guidToObjectMap[guid].go.name : "NULL"), (guidComponent != null ? guidComponent.name : "NULL"));
            }
            else
            {
                // however, at editor time, copying an object with a GUID will duplicate the GUID resulting in a collision and repair.
                // we warn about this just for pedantry reasons, and so you can detect if you are unexpectedly copying these components
                Debug.LogWarningFormat(guidComponent, "Guid Collision Detected while creating {0}.\nAssigning new Guid.", (guidComponent != null ? guidComponent.name : "NULL"));
            }
            return false;
        }

        // if we already tried to find this GUID, but haven't set the game object to anything specific, copy any OnAdd callbacks then call them
        existingInfo.go = info.go;
        existingInfo.HandleAddCallback();
        guidToObjectMap[guid] = existingInfo;
        return true;
    }

    private void InternalRemove(System.Guid guid)
    {
        GuidInfo info;
        if (guidToObjectMap.TryGetValue(guid, out info))
        {
            // trigger all the destroy delegates that have registered
            info.HandleRemoveCallback();
        }

        guidToObjectMap.Remove(guid);
    }

    // nice easy api to find a GUID, and if it works, register an on destroy callback
    // this should be used to register functions to cleanup any data you cache on finding
    // your target. Otherwise, you might keep components in memory by referencing them
    private GameObject ResolveGuidInternal(System.Guid guid, Action<GameObject> onAddCallback, Action onRemoveCallback)
    {
        GuidInfo info;
        if (guidToObjectMap.TryGetValue(guid, out info))
        {
            if (onAddCallback != null)
            {
                info.OnAdd += onAddCallback;
            }

            if (onRemoveCallback != null)
            {
                info.OnRemove += onRemoveCallback;
            }
            guidToObjectMap[guid] = info;
            return info.go;
        }

        if (onAddCallback != null)
        {
            info.OnAdd += onAddCallback;
        }

        if (onRemoveCallback != null)
        {
            info.OnRemove += onRemoveCallback;
        }

        guidToObjectMap.Add(guid, info);
        
        return null;
    }

    private GameObject ResolveGuidRuntimeInternal(Guid guid)
    {
        GuidInfo info;
        if (guidToObjectMapRuntime.TryGetValue(guid, out info))
        {
            guidToObjectMapRuntime[guid] = info;
            return info.go;
        }

        guidToObjectMapRuntime.Add(guid, info);
        return null;
    }

    public static GameObject ResolveGuidRuntime(Guid guid)
    {
        return Instance.ResolveGuidRuntimeInternal(guid);
    }
}
