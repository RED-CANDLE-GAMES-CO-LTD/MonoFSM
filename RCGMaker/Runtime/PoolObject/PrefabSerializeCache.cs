using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using UnityEngine;

namespace RCGMaker.Runtime
{
    public class PrefabSerializeCache : MonoBehaviour, IEditorOnly, IBeforePrefabSaveCallbackReceiver
    {
        public MonoReferenceCache _monoReferenceCache;

        public void OnBeforePrefabSave()
        {
            var poolObject = GetComponentInParent<global::PoolObject>();
            if (poolObject == null)
            {
                Debug.LogError("PrefabSerializeCache must be a child of PoolObject");
                return;
            }


            _monoReferenceCache.StoreReferenceCache(poolObject.gameObject);
            //prewarm的PoolObject要用這個
        }

        //FIXME: call after prewarm

        private void RestoreReferenceCache()
        {
            _monoReferenceCache.RestoreReferenceCacheToMonos();
        }
    }
}