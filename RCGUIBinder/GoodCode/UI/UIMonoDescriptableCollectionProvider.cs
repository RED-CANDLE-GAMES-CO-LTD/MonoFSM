using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGUIBinder
{
    
    //Scriptable 內包含陣列
    //ex: requireItems Entry.Item(Descriptable), Entry.Count(int)
    
    //對應背包的slots
    //甚至更多
    
    //給UI用的，要把主角身上的資料綁過來
    public class UIMonoDescriptableCollectionProvider:MonoBehaviour,ILevelResetStart
    {
        [SOConfig("DescriptableTag")]
        public MonoDescriptableTag tag;
        
        
        //FIXME:同步數量，instantiate prefab?
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