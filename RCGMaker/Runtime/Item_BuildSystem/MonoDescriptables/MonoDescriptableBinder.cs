using RCGMaker.Core;
using RCGUIBinder;
using UnityEngine;

namespace RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables
{
    /// <summary>
    /// 用MonoDescriptableTag當key的來找IMonoDescriptable
    /// </summary>
    public class MonoDescriptableBinder:MonoDict<MonoDescriptableTag,IMonoDescriptable>
    {
        //FIXME: 直接用MonoDescriptable就好？
        protected override void RemoveImplement(IMonoDescriptable item)
        {
            
        }
        
    }
    
    public static class MonoDescriptableBinderExtension
    {
        public static MonoDescriptableBinder GetMonoBinder(this MonoBehaviour mono)
        {
            
            return mono.GetComponentInParent<MonoDescriptableBinder>();
        }
        public static MonoDescriptable GetMonoDescriptableInstance(this MonoBehaviour mono, MonoDescriptableTag tag)
        {
             var descriptable = mono.GetComponentInParent<MonoDescriptableBinder>().Get(tag);
             return descriptable as MonoDescriptable;
        }
    }
    
}