using System;
using System.Collections.Generic;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using RCGMaker.Runtime;
using RCGMaker.Runtime.Attributes;
using RCGMaker.Runtime.FSM._2_Variable;
using RCGMaker.Runtime.Item_BuildSystem;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using RCGUIBinder;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace RCGMaker.Core
{
    public interface IConfigVar
    {
        object GetValue();
    }


    [Serializable]
    public class FloatConfig : IConfigVar, IFloatValueProvider
    {
        public float value;

        object IConfigVar.GetValue()
        {
            return value;
        }

        public float FinalValue => value;
    }


    [Serializable]
    public class IntConfig : IConfigVar, IIntProvider
    {
        public int value;

        object IConfigVar.GetValue()
        {
            return value;
        }

        // public int FinalValue => value;
        public int IntValue => value;
    }


    [Serializable]
    public class DescriptablePropertyProvider<T> where T : class
    {
        [SerializeReference] public IDescriptableDataProvider dataProvider;
        private ValueDropdownList<string> GetPropertyNames => dataProvider.GetDescriptableData().GetProperties<T>();

        [ValueDropdown(nameof(GetPropertyNames))]
        public string propertyName;

        public T GetValue()
        {
            return dataProvider.GetDescriptableData().GetProperty(propertyName) as T;
        }
    }

    //選擇一個 Data的Property
    [Serializable]
    public abstract class AbstractDescriptablePropertyProvider
    {
        protected abstract List<Type> supportedTypes { get; }
        [SerializeReference] public IDescriptableDataProvider _dataProvider;

        [PreviewInInspector] private IDescriptableData data => _dataProvider.GetDescriptableData();

        public DescriptableData SampleData;
        private ValueDropdownList<string> GetPropertyNames => SampleData?.GetProperties(supportedTypes);

        [ValueDropdown(nameof(GetPropertyNames))]
        public string _propertyName;
        // public T GetValue()
        // {
        //     return dataProvider.GetDescriptableData().GetProperty(propertyName) as T;
        // }
    }

    public interface IDescriptableDataProvider
    {
        public IDescriptableData GetDescriptableData();
        // public Type GetDescriptableType();
    }


    [Serializable]
    public class DescriptableDataConfig : IConfigVar //IObjectProvider?
    {
        [SerializeReference] public IDescriptableDataProvider data;

        object IConfigVar.GetValue()
        {
            return data.GetDescriptableData();
        }
    }

    [Serializable]
    public class DescriptableDataFromMonoDescriptable : IDescriptableDataProvider
    {
        // [SerializeReference]
        [DropDownRef] public MonoDescriptable _monoDescriptable;

        public IDescriptableData GetDescriptableData()
        {
            if (_monoDescriptable == null) return null;
            return _monoDescriptable.Descriptable;
        }

        public Type GetDescriptableType()
        {
            return _monoDescriptable.Descriptable.GetType();
        }
    }

    [Serializable]
    public class DescriptableDataFromMonoDescriptableInjector : IDescriptableDataProvider
    {
        [HideInInspector] [SerializeReferenceParentValidate]
        public MonoBehaviour propertyParent;

        public IDescriptableData GetDescriptableData()
        {
            if (propertyParent == null) return null;
            var injector = propertyParent.GetComponentInParent<UIMonoDescriptableProvider>();
            return injector.CurrentInstance;
        }
    }

    [Serializable]
    public class DescriptableDataReference : IDescriptableDataProvider
    {
        [SerializeField] private DescriptableData _data;

        public IDescriptableData GetDescriptableData()
        {
            return _data;
        }
    }

    [Serializable]
    public class DescriptableDataFromVariable : IDescriptableDataProvider
    {
        [DropDownRef] public AbstractObjectVariable _variable;

        public IDescriptableData GetDescriptableData()
        {
            return _variable.RawValue as IDescriptableData;
        }
    }
}