using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.Core.Attributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.FSM._3_FlagData;
using MonoFSM.Runtime.Item_BuildSystem;
using MonoFSM.Runtime.Item_BuildSystem.MonoDescriptables;
using MonoFSM.Runtime.Mono;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIValueBinder
{
    public interface IDescriptableProvider //FIXME: 這樣只能對應到一個instance, 而不能隨意查找
    {
        ValueDropdownList<string> GetProperties(List<Type> supportedTypes);
        object GetPropertyValue(IDescriptableData data, string propertyName);
        IDescriptableData SampleData { get; }
        IDescriptableData CurrentInstance { get; }
        object GetInstanceProperty(string fieldName);
    }

    //FIXME: 好像不需要了？應該從拿valueBinder直接assign => 有tag就可以拿到
    //目的：提供一個MonoDescriptable，可以從外部注入Descriptable，或是從一個collection拿到
    //FIXME: Consumer? 重點是從Collection拿
    [Searchable]
    public class UIMonoDescriptableProvider : MonoBehaviour, IDescriptableProvider, IResetStart
    {
        public enum SourceType
        {
            MonoTag,
            CollectionIndex
        }

        [Header("DI注入MonoDescriptable")] public SourceType sourceType; //FIXME: 把這個做完

        [FormerlySerializedAs("tag")] [ShowIf(nameof(sourceType), SourceType.MonoTag)] [SOConfig("DescriptableTag")]
        public MonoEntityTag monoTag; //我就是provider...

        [ShowIf(nameof(sourceType), SourceType.MonoTag)] [PreviewInInspector]
        private MonoEntity _bindedEntity; //單一型 

        // [ShowIf(nameof(sourceType),SourceType.MonoTag)]
        [Required] //FIXME: 一定要有sampleData才能選property?
        public DescriptableData SampleItemData;
        //從上面怎麼灌到？
        //怎麼DI綁這個？

        [ShowIf(nameof(sourceType), SourceType.CollectionIndex)] [PreviewInInspector] [AutoParent]
        UIMonoDescriptableCollectionProvider collectionProvider; //用provider

        // [TabGroup("WithCollection")]
        // [SerializeField] GameFlagCollection collection;//直接拉Data
        [ShowIf(nameof(sourceType), SourceType.CollectionIndex)]
        public int index; //陣列型

        // [PreviewInInspector]
        // string instanceFrom
        // {
        //     get
        //     {
        //         if(collectionProvider != null)
        //             return "collectionProvider";
        //         return "bindedDescriptable";
        //     }
        // }


        [GUIColor(0.2f, 0.8f, 0.2f)]
        [PreviewInInspector]
        public virtual MonoEntity MonoInstance
        {
            get
            {
                //FIXME: 會因為沒有play資料還沒初始化？
                // if (!Application.isPlaying)
                //     return null;
                if (sourceType == SourceType.CollectionIndex && collectionProvider != null)
                {
                    return collectionProvider.GetDescriptable(index);
                }

                return _bindedEntity;
            }
        }

        // public IDescriptable Descriptable => monoInstance.Descriptable;
        public ValueDropdownList<string> GetProperties(List<Type> supportedTypes)
        {
            // AppDomain.CurrentDomain.GetAssemblies().
            if (SampleData == null)
                return new ValueDropdownList<string>();
            var type = SampleData.GetType();
            // Debug.Log(type);
            var fields = new List<string>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dropdownList = new ValueDropdownList<string>();
            foreach (var property in properties)
            {
                if (!supportedTypes.Contains(property.PropertyType))
                    continue;
                fields.Add(property.Name);
                dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
            }

            return dropdownList;
        }

        public static ValueDropdownList<string> GetProperties(object obj, List<Type> supportedTypes,
            bool isArray = false)
        {
            return GetProperties(obj.GetType(), supportedTypes, isArray);
        }

        public static ValueDropdownList<string> GetProperties(Type type, List<Type> supportedTypes,
            bool isArray = false)
        {
            // AppDomain.CurrentDomain.GetAssemblies().

            // Debug.Log(type);
            var fields = new List<string>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dropdownList = new ValueDropdownList<string>();
            foreach (var property in properties)
            {
                if (isArray && !property.PropertyType.IsArray)
                {
                    // fields.Add(property.Name);
                    // dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
                    continue;
                }

                if (supportedTypes != null && !supportedTypes.Contains(property.PropertyType))
                    continue;
                fields.Add(property.Name);
                dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
            }

            return dropdownList;
        }

        //nested reflection
        //a.b.c.d
        //a.b[i].c.d[i]


        public IDescriptableData SampleData => SampleItemData;

        public IDescriptableData CurrentInstance
        {
            get
            {
                if (MonoInstance == null)
                {
                    if (Application.isPlaying == false)
                        return SampleData;
                    Debug.LogError("No monoInstance found", this);
                    return null;
                }

                return MonoInstance.Descriptable;
            }
        }

        public object GetInstanceProperty(string fieldName)
        {
            return MonoInstance.GetPropertyCache(fieldName)?.Invoke(MonoInstance);
        }

        public object GetPropertyValue(IDescriptableData data, string propertyName)
        {
            return data.GetPropertyCache(propertyName)?.Invoke(data);
        }

        //FIXME: 更新UI另外拉出去做？ UIValueUpdater?
        // [PreviewInInspector] [AutoChildren] private AbstractUIValueBinder[] _additionalDisplayers;

        // private void Update()
        // {
        //     if(monoInstance == null)
        //         return;
        //     foreach (var displayer in _additionalDisplayers)
        //     {
        //         displayer.UpdateView(CurrentInstance);
        //     }
        // }

        public void BindDescriptable(IMonoDescriptable descriptable)
        {
        }

        [PreviewInInspector] [AutoParent] private MonoEntityBinder _binder;

        [Button]
        void Bind()
        {
            if (sourceType != SourceType.MonoTag)
            {
                return;
            }

            if (monoTag == null)
            {
                Debug.LogError("No tag found", this);
                return;
            }

            Debug.Log("Bind: " + monoTag, this);

            if (!_binder.Contains(monoTag))
            {
                Debug.LogError("No mono found: " + monoTag, this);
            }

            var mono = _binder.Get(monoTag);

            _bindedEntity = (MonoEntity)mono;
        }

        private void Start()
        {
            // Bind();
        }

        public void ResetStart()
        {
            Bind();
        }
    }
}