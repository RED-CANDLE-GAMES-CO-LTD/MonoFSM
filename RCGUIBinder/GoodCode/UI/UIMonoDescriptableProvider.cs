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
        [TabGroup("Single")]
        [SOConfig("DescriptableTag")]
        public MonoDescriptableTag tag;
        [TabGroup("Single")]
        [PreviewInInspector]
        MonoDescriptable bindedDescriptable; //單一型
        [TabGroup("Single")]
        public GameFlagDescriptable SampleItemData;
        //從上面怎麼灌到？
        //怎麼DI綁這個？
        
        [TabGroup("WithCollection")]
        [PreviewInInspector]
        [AutoParent]
        UIMonoDescriptableCollectionProvider collectionProvider;
        [TabGroup("WithCollection")]
        public int index; //陣列型
        
       
        public void BindDescriptable(IMonoDescriptable descriptable)
        {
            bindedDescriptable = (MonoDescriptable)descriptable;
        }

        [PreviewInInspector]
        public MonoDescriptable monoInstance => collectionProvider != null
            ? collectionProvider.GetDescriptable(index)
            : bindedDescriptable;
        public IDescriptable Descriptable => monoInstance.Descriptable;
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
        public IDescriptable CurrentInstance => monoInstance.Descriptable;
        public object GetInstanceProperty(string fieldName)
        {
            return monoInstance.GetPropertyCache(fieldName)?.Invoke(monoInstance);
        }
        public object GetProperty(IDescriptable data, string propertyName)
        {
            return data.GetPropertyCache(propertyName)?.Invoke(data);
        }
        
         [PreviewInInspector] [AutoChildren] private AbstractUIValueBinder[] _additionalDisplayers;

         private void Update()
         {
             if(monoInstance == null)
                 return;
             foreach (var displayer in _additionalDisplayers)
             {
                 displayer.UpdateView(CurrentInstance);
             }
         }
         
         [Button]
         void Bind()
         {
             if (tag != null)
             {
                 var instance = GetComponentInParent<MonoDescriptableBinder>().Get(tag);
                 BindDescriptable(instance);
             }
             
         }

         public void LevelResetStart()
         {
             Bind();
         }
    }
}