using System;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIValueBinder
{
    //Scriptable 內包含陣列
    //ex: requireItems Entry.Item(Descriptable), Entry.Count(int)

    //對應背包的slots
    //甚至更多

    //給UI用的，要把主角身上的資料綁過來
    //從一個collection中取得某個Descriptable
    public class UIMonoDescriptableCollectionProvider : MonoBehaviour, IResetStart
    {
        //ItemCollection?
        [TabGroup("Collection")] [SOConfig("DescriptableTag")]
        public MonoEntityTag _tag;

        //FIXME:同步數量，instantiate prefab?
        // [AutoChildren]
        // MonoDescriptableProvider[] _descriptableProviders;
        [Required] [TabGroup("Collection")] [PreviewInInspector]
        public IMonoDescriptableCollection MonoDescriptableCollection;

        public MonoEntity GetDescriptable(int index)
        {
            if (MonoDescriptableCollection == null)
            {
                // Debug.LogError("MonoDescriptableCollection is null");
                return null;
            }

            return MonoDescriptableCollection.MonoDescriptableList[index] as MonoEntity;
        }

//FIXME: 要runtime才有用，dict還沒見建立好
        [Button]
        void Bind()
        {
            MonoDescriptableCollection = GetComponentInParent<MonoDescriptableCollectionBinder>().Get(_tag);
        }

        // private void Start()
        // {
        //     // Bind();
        // }

        public void ResetStart()
        {
            Bind();
        }
    }
}