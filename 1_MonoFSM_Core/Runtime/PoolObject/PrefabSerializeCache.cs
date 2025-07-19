using System;
using Auto_Attribute.Runtime;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime
{
    public interface IPrefabSerializeCacheOwner
    {
        GameObject gameObject { get; }
    }
    //從SceneSaveManager來重新處理prefab?
    //FIXME: 很容易沒跑到？Spawn 時要 autoReference 
    //獨立不好，和MonoPoolObj一起用
    [DefaultExecutionOrder(-20000)]
    public class PrefabSerializeCache : MonoBehaviour, IBeforePrefabSaveCallbackReceiver
    {
        [Title("singleton類的prefab, poolObject應該不可以用")]
        public bool _restoreAtAwake = true;

        [SerializeField] private MonoReferenceCache _monoReferenceCache;

//         private void Awake()
//         {
//             if (_restoreAtAwake)
//             {
// #if UNITY_EDITOR
//                 AutoAttributeManager.AutoReferenceAllChildren(_monoReferenceCache.RootObj);
// #else
//                 RestoreReferenceCache();
// #endif
//             }
//         }

        public void OnBeforePrefabSave()
        {
            var owner = GetComponentInParent<IPrefabSerializeCacheOwner>();
            if (owner == null)
            {
                Debug.LogError("PrefabSerializeCache must be a child of PoolObject");
                return;
            }

            _monoReferenceCache.RootObj = owner.gameObject;
            _monoReferenceCache.SaveReferenceCache();
            //prewarm的PoolObject要用這個
        }

        //FIXME: call after prewarm

        public void RestoreReferenceCache()
        {
            if (_monoReferenceCache.RootObj == null)
            {
                //FIXME: singleton會為賺嗎？
                var owner = GetComponentInParent<IPrefabSerializeCacheOwner>();
                Debug.LogError("RootObject is null");
                AutoAttributeManager.AutoReferenceAllChildren(owner.gameObject);
                return;
            }

            _monoReferenceCache.RestoreReferenceCacheToMonoFields();
        }
    }
}