using System;
using RCGMaker.Core;
using RCGMaker.Runtime.Mono;
using UnityEngine;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    /// <summary>
    /// 用MonoDescriptableTag當key的來找IMonoDescriptable
    /// </summary>
    public class MonoDescriptableBinder : MonoDict<MonoDescriptableTag, MonoDescriptable>
    {
        // protected override void Awake()
        // {
        //     base.Awake();
        //     Debug.Log("MonoDescriptableBinder Awake" + GetKeys.Count + ",collection:" + collections.Length, this);
        //     foreach (var key in GetKeys)
        //     {
        //         Debug.Log("MonoDescriptableBinder " + key, this[key]);
        //     }
        // }
        
        //network想要看authority來決定要不要加到字典裡...這個性質是什麼
        
        protected override bool IsAddValid(MonoDescriptable item)
        {
            if (item.TryGetComponent<IMonoAddToBinderChecker>(out var checker))
            {
                return checker.IsAddValid();
            }
            else
            {
                // Debug.Log("MonoDescriptableBinder IsAddValid " + item.name, item);
                return true;
            }
        }
        

        protected override bool isLog => true;

        //FIXME: 直接用MonoDescriptable就好？
        protected override void AddImplement(MonoDescriptable item)
        {
            // item.IsRegister = true;
        }

        protected override void RemoveImplement(MonoDescriptable item)
        {
        }

        protected override bool CanBeAdded(MonoDescriptable item)
        {
            return true;
            // return item.isActiveAndEnabled;
        }
    }

    public static class MonoDescriptableBinderExtension
    {
        public static MonoDescriptableBinder GetMonoBinder(this MonoBehaviour mono)
        {
            return mono.GetComponentInParent<MonoDescriptableBinder>();
        }

        //FIXME: 一個tag可能有多個instance? 要找最近的... 如果是經過parent的話？
        public static MonoDescriptable GetMonoCompInParent(this MonoBehaviour mono, MonoDescriptableTag tag)
        {
            //Descriptable就在自己的parent上，
            if (mono == null)
                return null;
            var parentDescriptable = mono.GetComponentInParent<MonoDescriptable>();
            if (parentDescriptable != null && parentDescriptable.Tag == tag || tag == null)
                return parentDescriptable;

            var parents = mono.GetComponentsInParent<MonoDescriptable>();
            foreach (var parent in parents)
            {
                if (parent.Tag == tag)
                {
                    return parent;
                }
            }

            //FIXME: 這個是從Binder往下找，可能有多個，不太好？
            // var binder = mono.GetComponentInParent<MonoDescriptableBinder>();
            // if (binder == null)
            // {
            //     // Debug.LogError("No MonoDescriptableBinder found "+tag,mono);
            //     return null;
            // }
            //
            // var descriptable = binder.Get(tag);
            // return descriptable;
            return null;
        }

        //TODO: 直接用Type來拿GlobalInstance..哪些需要？interface註冊？
        public static T GetGlobalInstance<T>(this MonoBehaviour mono, MonoDescriptableTag tag)
            where T : MonoDescriptable
        {
            return mono.GetGlobalInstance(tag) as T;
        }

        //類似singleton, 但是可能有多個世界，因此可以從某個容器底下找到唯一即可
        public static T GetGlobalInstance<T>(this MonoBehaviour mono) where T : MonoDescriptable, IGlobalInstance
        {
            var type = typeof(T);
            var binder = mono.GetComponentInParent<MonoDescriptableBinder>();
            if (binder == null)
            {
                Debug.LogError("No MonoDescriptableBinder found " + type, mono);
                return default;
            }

            var descriptable = binder.Get(type);
            if (descriptable == null)
                Debug.LogError("No MonoDescriptable found " + type, mono);
            return descriptable as T;
        }

        /// <summary>
        /// 從Binder找到GlobalInstance, 如果要特定Component (ex: MonoCharacter) 可以用GetComponent再拿看看 
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static MonoDescriptable GetGlobalInstance(this MonoBehaviour mono, MonoDescriptableTag tag)
        {
            //Descriptable就在自己的parent上，
            if (mono == null)
            {
                Debug.LogError("Mono is null");
                return null;
            }

            //FIXME: 這個是從Binder往下找，可能有多個，不太好？
            var binder = mono.GetComponentInParent<MonoDescriptableBinder>();
            if (binder == null)
            {
#if UNITY_EDITOR //如果在Prefab裡不要噴error
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                    return null;
#endif
                if(Application.isPlaying)
                    Debug.LogError("No MonoDescriptableBinder found " + tag, mono);
                return null;
            }

            if (tag == null)
                return null;
            var descriptable = binder.Get(tag);
            if (descriptable == null)
            {
                if (Application.isPlaying)
                    Debug.LogError(
                        $"No MonoDescriptable found with tag: {tag} (MonoBehaviour: {mono?.name}, Binder: {binder?.name})",
                        mono);
                // Debug.LogError("No MonoDescriptable found tag:" + tag, mono);
                // Debug.LogError("No MonoDescriptable found of tag: " + tag, binder);
            }

            // Debug.Log("GetGlobalInstance " + tag, descriptable);
            return descriptable;
            // return null;
        }

        //不需要provider?
        public static MonoDescriptable GetMonoCompInParent(this MonoBehaviour mono, string tag)
        {
            //FIXME: 效能不好？怎麼cache binder? 在弄一個dict? 樹狀結構改變呢？
            var binder = mono.GetComponentInParent<MonoDescriptableBinder>();
            if (binder == null)
            {
                Debug.LogError("No MonoDescriptableBinder found " + tag, mono);
                return null;
            }

            var descriptable = binder.Get(tag);
            return descriptable;
        }
        
       
    }
}