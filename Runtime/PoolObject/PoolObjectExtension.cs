public static class PoolObjectExtension
{
    public static PoolObject BorrowOrInstantiate(this PoolObject prefab)
    {
        return PoolManager.Instance.BorrowOrInstantiate(prefab, prefab.transform.position, prefab.transform.rotation);
    }
}