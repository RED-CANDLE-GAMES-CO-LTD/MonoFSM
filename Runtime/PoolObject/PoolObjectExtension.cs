using UnityEngine;

public static class PoolObjectExtension
{
    public static PoolObject BorrowOrInstantiate(this PoolObject prefab)
    {
        if (prefab == null) Debug.LogError("Prefab is null...");
        return PoolManager.Instance.BorrowOrInstantiate(prefab, prefab.transform.position, prefab.transform.rotation);
    }
}