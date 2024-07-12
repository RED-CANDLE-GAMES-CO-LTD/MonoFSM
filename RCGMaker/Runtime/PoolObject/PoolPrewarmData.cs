using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Searchable]
[CreateAssetMenu(fileName = "New PoolPrewarmData", menuName = "Boa/PoolManager/Create PoolPrewarmData", order = 3)]
public class PoolPrewarmData : ScriptableObject
{
    public List<PoolManager.PoolObjectEntry> objectEntries = new List<PoolManager.PoolObjectEntry>();
    public List<PoolManager.AddressableEntry> addressableRecords = new ();

    public GameObject TryFindPrefab(AssetReference targetReference)
    {
        addressableRecords.RemoveAll((a) => a._assetReference == null || a._prefab == null);
        
        foreach (var a in addressableRecords)
        {
            if (a._assetReference == targetReference)
            {
                return a._prefab;
            }
        }
        return null;
    }

    public void RegisterEntry(GameObject g,AssetReference asset)
    {
        #if UNITY_EDITOR
        addressableRecords.RemoveAll((a) => a._assetReference == null || a._prefab == null);
        
        foreach (var r in addressableRecords)
        {
            if (r._assetReference == asset)
                return;
        }

        addressableRecords.Add(new PoolManager.AddressableEntry(asset,g));
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    
    public void UpdatePoolObjectEntry(PoolObject poolObject, int count)
    {
#if UNITY_EDITOR
        foreach (var entry in objectEntries)
        {
            if (entry.prefab == poolObject)
            {
                if (count > entry.DefaultMaximumCount)
                {
                    Debug.LogError("Update max count for " + poolObject.name + " from " + entry.DefaultMaximumCount +
                                   " to " + count, this);
                    entry.DefaultMaximumCount = count;
                }

                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }
        }

        var newEntry = new PoolManager.PoolObjectEntry
        {
            prefab = poolObject,
            DefaultMaximumCount = count
        };
        objectEntries.Add(newEntry);
        Debug.LogError("Add new entry for " + poolObject.name);
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public void PrewarmObjects(PoolManager poolManager, MonoBehaviour owner)
    {
        poolManager.RegisterPoolPrewarmData(owner, this);

    }


}
