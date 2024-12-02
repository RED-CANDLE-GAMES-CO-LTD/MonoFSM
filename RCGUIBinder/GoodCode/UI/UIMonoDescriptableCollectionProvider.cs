using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGUIBinder
{
    //對應背包的slots
    //甚至更多
    
    //給UI用的，要把主角身上的資料綁過來
    public class UIMonoDescriptableCollectionProvider:MonoBehaviour,ILevelResetStart
    {
        public MonoDescriptableTag tag;
        //FIXME: 什麼時候DI過來？用type? 用拉的？ 某種type? binding, Solver
        // [AutoChildren]
        // MonoDescriptableProvider[] _descriptableProviders;
        [PreviewInInspector]
        public IMonoDescriptableCollection MonoDescriptableCollection; 
        public MonoDescriptable GetDescriptable(int index)
        {
            if(MonoDescriptableCollection == null)
            {
                // Debug.LogError("MonoDescriptableCollection is null");
                return null;
            }
            return MonoDescriptableCollection.MonoDescriptableList[index] as MonoDescriptable;
        }

        [Button]
        void Bind()
        {
            MonoDescriptableCollection = GetComponentInParent<MonoDescriptableCollectionBinder>().Get(tag);
        }

        public void LevelResetStart()
        {
            Bind();
        }
    }
}