using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MonoFSM.AddressableAssets;
using Sirenix.OdinInspector;

/// <summary>
/// Pool 管理需求記錄
/// </summary>
public class PoolObjectRequestRecords
{
    public PoolObjectRequestRecords(MonoBehaviour requester, PoolObject prefab, int count)
    {
        _requester = requester;
        _prefab = prefab;
        _count = count;
    }

    public void Clear()
    {
        _requester = null;
        _prefab = null;
        _count = 0;
    }

    public MonoBehaviour _requester;
    public PoolObject _prefab;
    public int _count = 0;
}

/// <summary>
/// Pool 物件配置條目
/// </summary>
[System.Serializable]
public class PoolObjectEntry
{
    // [OnValueChanged(nameof(OnPrefabChanged))]
    public PoolObject prefab;

    public string _cacheName;
    public int DefaultMaximumCount = 1;

    public void Clear()
    {
        prefab = null;
        DefaultMaximumCount = 0;
    }
}

/// <summary>
/// Addressable 資源條目
/// </summary>
[System.Serializable]
public class AddressableEntry
{
    public AssetReference _assetReference;
    public GameObject _prefab;

    public AddressableEntry(AssetReference assetReference, GameObject prefab)
    {
        _prefab = prefab;
        _assetReference = assetReference;
    }
}
