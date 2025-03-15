using UnityEngine;
using System;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

// This call is the type used by any other code to hold a reference to an object by GUID
// If the target object is loaded, it will be returned, otherwise, NULL will be returned
// This always works in Game Objects, so calling code will need to use GetComponent<>
// or other methods to track down the specific objects need by any given system

// Ideally this would be a struct, but we need the ISerializationCallbackReciever
[System.Serializable]
public class GuidReference : ISerializationCallbackReceiver, IEquatable<GuidReference>
{
    // cache the referenced Game Object if we find one for performance
    private GameObject cachedReference;
    private bool isCacheSet;
    
    // store our GUID in a form that Unity can save
    [HideInInspector]
    [SerializeField]
    private byte[] serializedGuid;


    private Guid guid;

    [ShowInInspector]
    [ReadOnly]
    public Guid Guid => guid;

#if UNITY_EDITOR
    // decorate with some extra info in Editor so we can inform a user of what that GUID means

    private bool isGameObjectNull => gameObject == null;

    [ShowIf(nameof(isGameObjectNull))]
    [SerializeField]
    private string cachedName;

    // [HideInInspector]
    // [SerializeField]
    // private SceneAsset cachedScene;
#endif

    // Set up events to let users register to cleanup their own cached references on destroy or to cache off values
    public event Action<GameObject> OnGuidAdded = delegate (GameObject go) { };
    public event Action OnGuidRemoved = delegate() { };

    // create concrete delegates to avoid boxing. 
    // When called 10,000 times, boxing would allocate ~1MB of GC Memory
    private Action<GameObject> addDelegate;
    private Action removeDelegate;

    public T Get<T>() where T : Component
    {
        return gameObject.GetComponent<T>();
    }

    [Button]
    private void ForceFetchRuntime()
    {
        runtimeCachedReference = GuidManager.ResolveGuidRuntime(guid);
    }
    // optimized accessor, and ideally the only code you ever call on this class
    [ShowInInspector]
    [ReadOnly]
    public GameObject gameObject //runtime會拿到runtime的物件，editor會拿到editor的物件
    {
        get
        {
            if (Application.isPlaying) //FIXME: runtime還是失敗？
            {
                if (isRuntimeCached && runtimeCachedReference != null)
                {
                    return runtimeCachedReference;
                }

                runtimeCachedReference = GuidManager.ResolveGuidRuntime(guid);

                isRuntimeCached = true;
                return runtimeCachedReference;
            }
            else
            {
                if (isCacheSet)
                {
                    return cachedReference;
                }

                cachedReference = GuidManager.ResolveGuid(guid, addDelegate, removeDelegate);
                if (cachedReference) //什麼時候會失敗？
                {
                    isCacheSet = true;
#if UNITY_EDITOR
                    cachedName = cachedReference.name;
#endif
                }
                return cachedReference;
            }
            
          
        }

        // private set {}
    }

    private bool isRuntimeCached = false;

    [DisableIf("@true")] [ShowInInspector]
    private GameObject runtimeCachedReference;

    // [ShowInInspector]
    // public GameObject RuntimeGameObject
    // {
    //     get
    //     {
    //         if (!Application.isPlaying)
    //         {
    //             return null;
    //         }
    //     }
    // }

    public GuidReference() { }

    public GuidReference(GuidComponent target)
    {
        guid = target.GetGuid();
    }

    public GuidReference(Guid guid)
    {
        this.guid = guid;
    }

    private void GuidAdded(GameObject go)
    {
        cachedReference = go;
        OnGuidAdded(go);
    }

    private void GuidRemoved()
    {
        cachedReference = null;
        isCacheSet = false;
        OnGuidRemoved?.Invoke();
    }

    //convert system guid to a format unity likes to work with
    public void OnBeforeSerialize()
    {
        serializedGuid = guid.ToByteArray();
    }

    // convert from byte array to system guid and reset state
    public void OnAfterDeserialize()
    {
        cachedReference = null;
        isCacheSet = false;
        if (serializedGuid == null || serializedGuid.Length != 16)
        {
            serializedGuid = new byte[16];
        }
        guid = new System.Guid(serializedGuid);
        addDelegate = GuidAdded;
        removeDelegate = GuidRemoved;
    }

    public bool Equals(GuidReference other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return guid.Equals(other.guid);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((GuidReference)obj);
    }

    public override int GetHashCode()
    {
        return guid.GetHashCode();
    }
}
