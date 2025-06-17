using System;
using System.Collections.Generic;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core.DataProvider
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

        private static List<Type> supportTypes = new() { typeof(string), typeof(int), typeof(float) };
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
        [SerializeReference] public IGameDataProvider dataProvider;
        private static List<Type> supportTypes = new() { typeof(string), typeof(int), typeof(float) };

        private ValueDropdownList<string> GetPropertyNames =>
            dataProvider.GameData.GetProperties(supportTypes);

        [ValueDropdown(nameof(GetPropertyNames))]
        public string propertyName;

        public string GetString()
        {
            return dataProvider.GameData.GetProperty(propertyName).ToString();
        }
    }
}