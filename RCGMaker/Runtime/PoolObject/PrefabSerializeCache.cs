using System;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime
{
    public interface IPrefabSerializeCacheOwner
    {
        GameObject gameObject { get; }
    }
    //從SceneSaveManager來重新處理prefab?
    public class PrefabSerializeCache : MonoBehaviour, IEditorOnly, IBeforePrefabSaveCallbackReceiver
    {
        [Title("singleton類的prefab, poolObject應該不可以用")]
        public bool RestoreAtAwake = false;

        [SerializeField] private MonoReferenceCache _monoReferenceCache;
      

        private void Awake()
        {
            if (RestoreAtAwake)
            {
                RestoreReferenceCache();
            }
        }

        public void OnBeforePrefabSave()
        {
            var owner = GetComponentInParent<IPrefabSerializeCacheOwner>();
            if (owner == null)
            {
                Debug.LogError("PrefabSerializeCache must be a child of PoolObject");
                return;
            }

            _monoReferenceCache.StoreReferenceCache(owner.gameObject);
            //prewarm的PoolObject要用這個
        }

        //FIXME: call after prewarm

        public void RestoreReferenceCache()
        {
            _monoReferenceCache.RestoreReferenceCacheToMonos();
        }
    }
}