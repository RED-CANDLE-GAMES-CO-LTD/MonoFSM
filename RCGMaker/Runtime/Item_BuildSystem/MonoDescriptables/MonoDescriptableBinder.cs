using RCGMaker.Core;
using UnityEngine;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    /// <summary>
    /// 用MonoDescriptableTag當key的來找IMonoDescriptable
    /// </summary>
    public class MonoDescriptableBinder:MonoDict<MonoDescriptableTag, MonoDescriptable>
    {
        //FIXME: 直接用MonoDescriptable就好？
        protected override void RemoveImplement(MonoDescriptable item)
        {
            
        }

        protected override bool CanBeAdded(MonoDescriptable item)
        {
            return item.isActiveAndEnabled;
        }
    }
    
    public static class MonoDescriptableBinderExtension
    {
        public static MonoDescriptableBinder GetMonoBinder(this MonoBehaviour mono)
        {
            
            return mono.GetComponentInParent<MonoDescriptableBinder>();
        }
        
        //FIXME: 一個tag可能有多個instance? 要找最近的... 如果是經過parent的話？
        public static MonoDescriptable GetMonoDescriptableInstance(this MonoBehaviour mono, MonoDescriptableTag tag)
        {
            //Descriptable就在自己的parent上，
            if(mono == null)
                return null;
            var parentDescriptable = mono.GetComponentInParent<MonoDescriptable>();
            if(parentDescriptable != null && parentDescriptable.Tag == tag || tag == null)
                return parentDescriptable;
            
            var binder = mono.GetComponentInParent<MonoDescriptableBinder>();
            if (binder == null)
            {
                // Debug.LogError("No MonoDescriptableBinder found "+tag,mono);
                return null;
            }
            var descriptable = binder.Get(tag);
            return descriptable;
            return null;
        }
        
        //不需要provider?
        public static MonoDescriptable GetMonoDescriptableInstance(this MonoBehaviour mono, string tag)
        {
            //FIXME: 效能不好？怎麼cache binder? 在弄一個dict? 樹狀結構改變呢？
            var binder = mono.GetComponentInParent<MonoDescriptableBinder>();
            if (binder == null)
            {
                Debug.LogError("No MonoDescriptableBinder found "+tag,mono);
                return null;
            }
            var descriptable = binder.Get(tag);
            return descriptable;
        }
    }
    
}