using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCGMaker.Core.DataProvider
{
    public interface IStringProvider
    {
        public string GetString();
    }

    [Serializable]
    public class StringProviderLiteral : IStringProvider
    {
        public string literal;

        public string GetString()
        {
            return literal;
        }
    }

    [Serializable]
    public class StringProviderFromVariable : IStringProvider
    {
        [FormerlySerializedAs("_variable")] [DropDownRef]
        public AbstractMonoVariable _monoVariable;

        public string GetString()
        {
            return _monoVariable.objectValue.ToString();
        }
    }

    [Serializable]
    public class StringProviderFromVariableProperty : IStringProvider
    {
        [FormerlySerializedAs("_variable")] [Required] [DropDownRef]
        public AbstractMonoVariable _monoVariable;

        static List<Type> supportTypes = new List<Type>() { typeof(string), typeof(int), typeof(float) };
        private ValueDropdownList<string> GetPropertyNames => DataReflection.GetProperties(_monoVariable, supportTypes);

        [Required] [ValueDropdown(nameof(GetPropertyNames))]
        public string propertyName;

        public string GetString()
        {
            return _monoVariable.GetProperty(propertyName).ToString(); //FIXME: cache?
        }
        //FIXME: event listener? 不polling就可以知道值改變
    }

    [Serializable]
    public class StringProviderFromDescriptableProperty : IStringProvider
    {
        [SerializeReference] public IDescriptableDataProvider dataProvider;
        static List<Type> supportTypes = new List<Type>() { typeof(string), typeof(int), typeof(float) };

        private ValueDropdownList<string> GetPropertyNames =>
            dataProvider.GetDescriptableData().GetProperties(supportTypes);

        [ValueDropdown(nameof(GetPropertyNames))]
        public string propertyName;

        public string GetString()
        {
            return dataProvider.GetDescriptableData().GetProperty(propertyName).ToString();
        }
    }
}