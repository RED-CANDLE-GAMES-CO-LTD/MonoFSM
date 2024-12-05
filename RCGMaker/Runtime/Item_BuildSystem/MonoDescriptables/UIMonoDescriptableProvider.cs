using System;
using System.Collections.Generic;
using System.Reflection;
using RCGMaker.Core.Attributes;
using RCGMaker.Runtime;
using RCGMaker.Runtime.FSM._3_FlagData;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGUIBinder
{
    public interface IDescriptableProvider
    {
        ValueDropdownList<string> GetProperties(List<Type> supportedTypes);
        object GetProperty(IDescriptable data, string propertyName);
        IDescriptable SampleData { get;  }
        IDescriptable CurrentInstance { get;  }
        object GetInstanceProperty(string fieldName);
    }
    
    
    //目的：提供一個DescriptableProvider，可以從外部注入Descriptable
    [Searchable]
    public class UIMonoDescriptableProvider:MonoBehaviour,IDescriptableProvider,ILevelResetStart
    {
        public enum SourceType
        {
            MonoTag,
            CollectionIndex
        }
        public SourceType sourceType; //FIXME: 把這個做完
        [ShowIf(nameof(sourceType),SourceType.MonoTag)]
        
        [SOConfig("DescriptableTag")]
        public MonoDescriptableTag tag; //我就是provider...
        
        [ShowIf(nameof(sourceType),SourceType.MonoTag)]
        
        [PreviewInInspector]
        MonoDescriptable bindedDescriptable; //單一型 
        
        [ShowIf(nameof(sourceType),SourceType.MonoTag)]
        public GameFlagDescriptable SampleItemData;
        //從上面怎麼灌到？
        //怎麼DI綁這個？
        
        [ShowIf(nameof(sourceType),SourceType.CollectionIndex)]
        [PreviewInInspector]
        [AutoParent]
        UIMonoDescriptableCollectionProvider collectionProvider; //用provider
        
        // [TabGroup("WithCollection")]
        // [SerializeField] GameFlagCollection collection;//直接拉Data
        [ShowIf(nameof(sourceType),SourceType.CollectionIndex)]
        public int index; //陣列型

        [PreviewInInspector]
        string instanceFrom
        {
            get
            {
                if(collectionProvider != null)
                    return "collectionProvider";
                return "bindedDescriptable";
            }
        }


        [PreviewInInspector]
        public MonoDescriptable monoInstance
        {
            get
            {
                if (sourceType == SourceType.CollectionIndex && collectionProvider != null)
                {
                    return collectionProvider.GetDescriptable(index);
                }
                return bindedDescriptable;
            }   
        } 
        // public IDescriptable Descriptable => monoInstance.Descriptable;
        public ValueDropdownList<string> GetProperties(List<Type> supportedTypes)
        {
            // AppDomain.CurrentDomain.GetAssemblies().
            if(SampleData == null)
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
        
        public static ValueDropdownList<string> GetProperties(object obj, List<Type> supportedTypes,bool isArray = false)
        {
            // AppDomain.CurrentDomain.GetAssemblies().
            var type = obj.GetType();
            // Debug.Log(type);
            var fields = new List<string>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dropdownList = new ValueDropdownList<string>();
            foreach (var property in properties)
            {
                if(isArray && !property.PropertyType.IsArray)
                {
                    // fields.Add(property.Name);
                    // dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
                    continue;
                }
                
                if (supportedTypes !=null && !supportedTypes.Contains(property.PropertyType))
                    continue;
                fields.Add(property.Name);
                dropdownList.Add(property.Name + " (" + property.PropertyType.Name + ")", property.Name);
            }
            return dropdownList;
        }
        
        //nested reflection
        //a.b.c.d
        //a.b[i].c.d[i]

      

        public IDescriptable SampleData => SampleItemData;
        public IDescriptable CurrentInstance
        {
            get
            {
                if (monoInstance == null)
                {
                    Debug.LogError("No monoInstance found", this);
                    return null;
                }
                    
                return monoInstance.Descriptable;
            }
        }

        public object GetInstanceProperty(string fieldName)
        {
            return monoInstance.GetPropertyCache(fieldName)?.Invoke(monoInstance);
        }
        public object GetProperty(IDescriptable data, string propertyName)
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
         
         [PreviewInInspector]
         [AutoParent] MonoDescriptableBinder _binder;
         
         [Button]
         void Bind()
         {
             if (tag == null)
             {
                 Debug.LogError("No tag found", this);
                 return;
             }

             Debug.Log("Bind: "+tag,this);

             if (!_binder.Contains(tag))
             {
                 Debug.LogError("No mono found "+tag, this);
             }
             var mono = _binder.Get(tag);
             
             bindedDescriptable = (MonoDescriptable)mono;
         }

         public void LevelResetStart()
         {
             Bind();
         }
    }
}