using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
#endif

// This component gives a GameObject a stable, non-replicatable Globally Unique IDentifier.
// It can be used to reference a specific instance of an object no matter where it is.
// This can also be used for other systems, such as Save/Load game
[DefaultExecutionOrder(-1000)]
[ExecuteInEditMode, DisallowMultipleComponent]
public class GuidComponent : MonoBehaviour, ISerializationCallbackReceiver
{
    // System guid we use for comparison and generation
    protected Guid guid = Guid.Empty;
    public Guid Guid => guid;

    // Unity's serialization system doesn't know about System.Guid, so we convert to a byte array
    // Fun fact, we tried using strings at first, but that allocated memory and was twice as slow
    [HideInInspector]
    [SerializeField]
    private byte[] serializedGuid;


    public bool IsGuidAssigned()
    {
        return guid != System.Guid.Empty;
    }

// #if UNITY_EDITOR
//     private void Awake()
//     {
//         var success = GuidManager.AddRuntime(this);
//         if (success == false)
//             Debug.Log("AddRuntime GUID Fail:", this);
//     }
// #endif

    // When de-serializing or creating this component, we want to either restore our serialized GUID
    // or create a new one.
    void CreateGuid()
    {
        // if our serialized data is invalid, then we are a new object and need a new GUID
        if (serializedGuid == null || serializedGuid.Length != 16)
        {
#if UNITY_EDITOR
            // if in editor, make sure we aren't a prefab of some kind
            if (IsAssetOnDisk())
            {
                return;
            }
            Undo.RecordObject(this, "Added GUID");
#endif
            guid = System.Guid.NewGuid();
            serializedGuid = guid.ToByteArray();

#if UNITY_EDITOR
            // If we are creating a new GUID for a prefab instance of a prefab, but we have somehow lost our prefab connection
            // force a save of the modified prefab instance properties
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
#endif
        }
        else if (guid == System.Guid.Empty)
        {
            // otherwise, we should set our system guid to our serialized guid
            guid = new System.Guid(serializedGuid);
        }

        // register with the GUID Manager so that other components can access this
        if (guid != System.Guid.Empty)
        {
            if (!GuidManager.Add(this))
            {
                // if registration fails, we probably have a duplicate or invalid GUID, get us a new one.

                if (Application
                    .isPlaying) //don't generate GUID in play mode, same scene can be loaded twice when reload
                    return;
                serializedGuid = null;
                guid = System.Guid.Empty;
                CreateGuid(); //遞迴直到成功
            }
        }
    }

#if UNITY_EDITOR
    private bool IsEditingInPrefabMode()
    {
        if (EditorUtility.IsPersistent(this))
        {
            // if the game object is stored on disk, it is a prefab of some kind, despite not returning true for IsPartOfPrefabAsset =/
            return true;
        }
        else
        {
            // If the GameObject is not persistent let's determine which stage we are in first because getting Prefab info depends on it
            var mainStage = StageUtility.GetMainStageHandle();
            var currentStage = StageUtility.GetStageHandle(gameObject);
            if (currentStage != mainStage)
            {
                var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
                if (prefabStage != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    protected bool IsAssetOnDisk()
    {
        try
        {
            return PrefabUtility.IsPartOfPrefabAsset(this) || IsEditingInPrefabMode();
        }
        catch
        {
            // Debug.LogError("IsAssetOnDisk??? 是prefab?", this);
            // if we are in the middle of a recompile, this will throw an exception, so we just ignore it
            return true;
        }
    }
#endif

    // We cannot allow a GUID to be saved into a prefab, and we need to convert to byte[]
    public virtual void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        // This lets us detect if we are a prefab instance or a prefab asset.
        // A prefab asset cannot contain a GUID since it would then be duplicated when instanced.
        if (IsAssetOnDisk())
        {
            serializedGuid = null;
            guid = System.Guid.Empty;
        }
        else
#endif
        {
            if (guid != System.Guid.Empty)
            {
                serializedGuid = guid.ToByteArray();
            }
        }
    }

    // On load, we can go head a restore our system guid for later use
    public virtual void OnAfterDeserialize()
    {
        if (serializedGuid != null && serializedGuid.Length == 16)
        {
            guid = new System.Guid(serializedGuid);
        }
    }

    // void Awake()
    // {
    //     CreateGuid();
    // }

    void OnValidate()
    {
#if UNITY_EDITOR
        // similar to on Serialize, but gets called on Copying a Component or Applying a Prefab
        // at a time that lets us detect what we are
        if (IsAssetOnDisk())
        {
            serializedGuid = null;
            guid = System.Guid.Empty;
        }
        else
#endif
        {
            if (Application.isPlaying)
                return;
            CreateGuid();
        }
    }

    // Never return an invalid GUID
    public Guid GetGuid()
    {
        if (guid == Guid.Empty && serializedGuid != null && serializedGuid.Length == 16)
        {
            guid = new System.Guid(serializedGuid);
            Debug.Log("GetGuid() GUID is empty, but serializedGuid is not null, restore GUID from serializedGuid");
            serializedGuid = guid.ToByteArray();
            return guid;
        }


        //runtime GUID (PoolObject用) 沒有要進guid系統，只是為了可以註冊
        if (guid == Guid.Empty && Application.isPlaying)
        {
            // Debug.Log("GetGuid() GUID is empty, but serializedGuid is not null, try to create new GUID", this);
            guid = Guid.NewGuid();
            serializedGuid = guid.ToByteArray();
            return guid;
        }

        return guid;
    }
    //FIXME: 直接寫一個dynamic GUID?

    // let the manager know we are gone, so other objects no longer find this
    public void OnDestroy()
    {
        if (Application.isPlaying)
            return;
        GuidManager.Remove(guid);
    }
}
