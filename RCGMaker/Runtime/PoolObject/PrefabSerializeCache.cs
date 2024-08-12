using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime
{
    //從SceneSaveManager來重新處理prefab?
    public class PrefabSerializeCache : MonoBehaviour, IEditorOnly, IBeforePrefabSaveCallbackReceiver
    {
        [SerializeField] private MonoReferenceCache _monoReferenceCache;

        public void OnBeforePrefabSave()
        {
            var poolObject = GetComponentInParent<PoolObject>();
            if (poolObject == null)
            {
                Debug.LogError("PrefabSerializeCache must be a child of PoolObject");
                return;
            }
            _monoReferenceCache.StoreReferenceCache(poolObject.gameObject);
            //prewarm的PoolObject要用這個
        }

        //FIXME: call after prewarm

        public void RestoreReferenceCache()
        {
            _monoReferenceCache.RestoreReferenceCacheToMonos();
        }
    }
}